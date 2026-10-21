import 'dart:convert';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../firebase_options.dart';
import 'models.dart';
import 'notice_watch.dart';
import 'session.dart';

/// School notices on the phone, two ways:
///  * Firebase set up (flutterfire configure wrote firebase_options.dart):
///    the server pushes each announcement at once to a topic
///    (`es_SSMHS_parents`, `es_SSMHS_c2_staff`); this phone subscribes to
///    exactly the topics GET /app/me lists.
///  * No Firebase yet: NoticeWatch checks the server about every 15 minutes
///    in the background and notifies new notices itself.
/// Tapping a notification opens that notice either way.
class Push {
  Push._();
  static final instance = Push._();

  static const _storage = FlutterSecureStorage();
  static const _kTopics = 'push_topics';

  final _local = FlutterLocalNotificationsPlugin();

  /// Firebase is configured and running.
  bool ready = false;
  bool _localReady = false;
  Session? _session;

  /// Set by the home screen: opens an announcement when its notification is tapped.
  void Function(int announcementId)? onOpenAnnouncement;
  int? _pendingOpen;

  bool get _android => !kIsWeb && defaultTargetPlatform == TargetPlatform.android;

  Future<void> init(Session session) async {
    _session = session;
    if (!_android) return;

    try {
      await _local.initialize(
        settings: const InitializationSettings(android: AndroidInitializationSettings('@drawable/ic_notification')),
        onDidReceiveNotificationResponse: (r) => _open(r.payload),
      );
      await _local.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()?.createNotificationChannel(NoticeWatch.channel);
      _localReady = true;
      // Opened by tapping a notice the background check posted.
      final launch = await _local.getNotificationAppLaunchDetails();
      if (launch?.didNotificationLaunchApp == true) _open(launch!.notificationResponse?.payload);
    } catch (e) {
      debugPrint('Local notifications off: $e');
    }

    try {
      await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
    } catch (e) {
      debugPrint('Firebase not set up - notices come from the background check: $e');
      return;
    }
    ready = true;

    // In the app: Android shows nothing by itself, so show it here.
    FirebaseMessaging.onMessage.listen((m) {
      final n = m.notification;
      if (n == null || !_localReady) return;
      _local.show(
        id: m.hashCode,
        title: n.title,
        body: n.body,
        notificationDetails: NotificationDetails(
          android: AndroidNotificationDetails(NoticeWatch.channel.id, NoticeWatch.channel.name,
              channelDescription: NoticeWatch.channel.description, importance: Importance.high, priority: Priority.high,
              icon: '@drawable/ic_notification', styleInformation: BigTextStyleInformation(n.body ?? '')),
        ),
        payload: jsonEncode(m.data),
      );
    });
    FirebaseMessaging.onMessageOpenedApp.listen((m) => _open(jsonEncode(m.data)));
    final first = await FirebaseMessaging.instance.getInitialMessage();
    if (first != null) _open(jsonEncode(first.data));
    FirebaseMessaging.instance.onTokenRefresh.listen((_) => _registerDevice());
  }

  void _open(String? payload) {
    if (payload == null) return;
    try {
      final data = jsonDecode(payload) as Map<String, dynamic>;
      if (data['type'] != 'announcement') return;
      final id = int.tryParse('${data['id']}');
      if (id == null) return;
      if (onOpenAnnouncement != null) {
        onOpenAnnouncement!(id);
      } else {
        _pendingOpen = id;
      }
    } catch (_) {}
  }

  /// The home screen calls this once it can show an announcement.
  int? takePendingOpen() {
    final id = _pendingOpen;
    _pendingOpen = null;
    return id;
  }

  /// After sign-in, every launch and sign-out.
  Future<void> sync(Me? me) async {
    if (!_android) return;
    try {
      if (me != null) {
        // Android 13+ asks the user once.
        await _local.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()?.requestNotificationsPermission();
      }
      if (!ready) {
        me != null ? await NoticeWatch.start() : await NoticeWatch.stop();
        return;
      }
      final want = me?.topics.toSet() ?? <String>{};
      final had = ((jsonDecode(await _storage.read(key: _kTopics) ?? '[]') as List).cast<String>()).toSet();
      for (final t in had.difference(want)) {
        await FirebaseMessaging.instance.unsubscribeFromTopic(t);
      }
      for (final t in want.difference(had)) {
        await FirebaseMessaging.instance.subscribeToTopic(t);
      }
      await _storage.write(key: _kTopics, value: jsonEncode(want.toList()));
      if (me != null) await _registerDevice();
    } catch (e) {
      debugPrint('Push sync failed: $e');
    }
  }

  Future<void> _registerDevice() async {
    final s = _session;
    if (!ready || s == null || !s.signedIn) return;
    final token = await FirebaseMessaging.instance.getToken();
    if (token == null) return;
    await s.api.post('/app/devices', {'token': token, 'platform': 'android', 'appVersion': appVersion});
  }

  /// While still signed in: forget this phone on the server.
  Future<void> beforeSignOut() async {
    if (!ready || _session == null) return;
    final token = await FirebaseMessaging.instance.getToken();
    if (token != null) await _session!.api.delete('/app/devices', query: {'token': token});
  }

  static String appVersion = '';
}
