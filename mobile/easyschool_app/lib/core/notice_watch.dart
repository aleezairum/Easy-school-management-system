import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:workmanager/workmanager.dart';

/// Runs in the background (Android WorkManager, about every 15 minutes while
/// the phone has internet), even when the app is closed.
@pragma('vm:entry-point')
void noticeWatchDispatcher() {
  Workmanager().executeTask((task, input) async {
    WidgetsFlutterBinding.ensureInitialized();
    try {
      await NoticeWatch.check();
    } catch (e) {
      debugPrint('Notice check failed: $e');
    }
    return true;
  });
}

/// School notices as phone notifications WITHOUT Firebase: the phone itself
/// asks the school's server for the current notices every ~15 minutes and
/// notifies the ones it has not seen. Used until Firebase is set up (then
/// Push sends them instantly and this is switched off).
///
/// What the app has already shown (Notices tab, home screen) counts as seen,
/// and on the first check after signing in the notices already running are
/// taken as seen - only new ones make a notification.
class NoticeWatch {
  static const _storage = FlutterSecureStorage();
  static const _kSeen = 'notice_seen';
  static const _task = 'es-notice-check';

  // Same keys as Session.
  static const _kSchool = 'school', _kToken = 'token';

  static const channel = AndroidNotificationChannel(
    'announcements',
    'School announcements',
    description: 'Notices from the school',
    importance: Importance.high,
  );

  static Future<void> start() async {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) return;
    try {
      await Workmanager().initialize(noticeWatchDispatcher);
      await Workmanager().registerPeriodicTask(
        _task,
        _task,
        frequency: const Duration(minutes: 15),
        constraints: Constraints(networkType: NetworkType.connected),
        existingWorkPolicy: ExistingPeriodicWorkPolicy.keep,
      );
    } catch (e) {
      debugPrint('Notice watch not started: $e');
    }
  }

  static Future<void> stop() async {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) return;
    try {
      await Workmanager().cancelByUniqueName(_task);
    } catch (_) {}
    await _storage.delete(key: _kSeen);
  }

  /// The app has shown these: no notification for them later.
  static Future<void> markSeen(Iterable<int> ids) async {
    if (kIsWeb) return;
    try {
      final raw = await _storage.read(key: _kSeen);
      final seen = raw == null ? <int>{} : (jsonDecode(raw) as List).cast<int>().toSet();
      await _write(seen..addAll(ids));
    } catch (_) {}
  }

  static Future<void> _write(Set<int> ids) async {
    final list = ids.toList()..sort();
    await _storage.write(key: _kSeen, value: jsonEncode(list.length > 300 ? list.sublist(list.length - 300) : list));
  }

  static Future<void> check() async {
    final school = await _storage.read(key: _kSchool);
    final token = await _storage.read(key: _kToken);
    if (school == null || token == null) return;
    final base = (jsonDecode(school) as Map<String, dynamic>)['apiBase'] as String;

    final res = await http.get(Uri.parse('$base/app/announcements'), headers: {
      'Accept': 'application/json',
      'Authorization': 'Bearer $token',
    }).timeout(const Duration(seconds: 25));
    if (res.statusCode != 200) return;
    final list = (jsonDecode(utf8.decode(res.bodyBytes)) as List).cast<Map<String, dynamic>>();
    final ids = list.map((e) => e['id'] as int).toSet();

    final raw = await _storage.read(key: _kSeen);
    if (raw == null) {
      await _write(ids); // first check: what is already running is not news
      return;
    }
    final seen = (jsonDecode(raw) as List).cast<int>().toSet();
    final fresh = list.where((e) => !seen.contains(e['id'])).take(3).toList();
    if (fresh.isEmpty) return;

    final plugin = FlutterLocalNotificationsPlugin();
    await plugin.initialize(settings: const InitializationSettings(android: AndroidInitializationSettings('@drawable/ic_notification')));
    await plugin.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()?.createNotificationChannel(channel);
    for (final a in fresh) {
      final body = (a['message'] as String? ?? '').trim();
      await plugin.show(
        id: a['id'] as int,
        title: a['title'] as String? ?? 'School notice',
        body: body.length > 180 ? '${body.substring(0, 177)}...' : body,
        notificationDetails: NotificationDetails(
          android: AndroidNotificationDetails(channel.id, channel.name,
              channelDescription: channel.description, importance: Importance.high, priority: Priority.high,
              icon: '@drawable/ic_notification', styleInformation: BigTextStyleInformation(body)),
        ),
        payload: jsonEncode({'type': 'announcement', 'id': '${a['id']}'}),
      );
    }
    await _write(seen..addAll(ids));
  }
}
