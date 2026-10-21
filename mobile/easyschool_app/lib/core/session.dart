import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'api.dart';
import 'models.dart';
import 'schools.dart';

/// Who is signed in, to which school. Kept on the phone (encrypted) so the
/// app opens straight to the home screen next time.
class Session extends ChangeNotifier {
  static const _storage = FlutterSecureStorage();
  static const _kSchool = 'school', _kInfo = 'school_info', _kToken = 'token', _kMust = 'must_change';

  bool ready = false;
  SchoolEntry? school;
  SchoolInfo? info;
  String? token;
  Me? me;
  bool mustChangePassword = false;

  /// Shown on the sign-in screen after the server ended the session.
  String? signedOutReason;

  Api? _api;
  Api get api => _api!;

  bool get hasSchool => school != null && info != null;
  bool get signedIn => token != null && me != null;

  void Function(Me? me)? onMeChanged;

  Api _makeApi(String base) => Api(base, token: token, onUnauthorized: () => signOut(reason: 'Your session has ended. Please sign in again.'));

  Future<void> load() async {
    try {
      final s = await _storage.read(key: _kSchool);
      final i = await _storage.read(key: _kInfo);
      if (s != null && i != null) {
        school = SchoolEntry.fromJson(jsonDecode(s));
        info = SchoolInfo.fromJson(jsonDecode(i));
        token = await _storage.read(key: _kToken);
        mustChangePassword = await _storage.read(key: _kMust) == '1';
        _api = _makeApi(school!.apiBase);
        if (token != null) {
          try {
            me = Me.fromJson(await api.get('/app/me'));
            refreshInfo();
          } on ApiException catch (e) {
            // Offline: keep the token and try again from the home screen.
            if (e.status == 401 || e.status == 403) {
              token = null;
              await _storage.delete(key: _kToken);
            }
          }
        }
      }
    } catch (_) {
      // Unreadable storage (restored backup on a new phone): start again.
      await _storage.deleteAll();
      school = null;
      info = null;
      token = null;
    }
    ready = true;
    notifyListeners();
    onMeChanged?.call(me);
  }

  /// Checks the school's server answers and is the school the code belongs to.
  Future<SchoolInfo> probe(SchoolEntry entry) async {
    final i = SchoolInfo.fromJson(await Api(entry.apiBase).get('/app/school-info'));
    if (entry.code.isNotEmpty && (i.schoolCode ?? '').toUpperCase() != entry.code) {
      throw ApiException(0, 'This server belongs to a different school (${i.schoolCode ?? 'no code'}).');
    }
    return i;
  }

  Future<void> chooseSchool(SchoolEntry entry, SchoolInfo i) async {
    school = SchoolEntry(code: (i.schoolCode ?? entry.code).toUpperCase(), name: i.name, apiBase: entry.apiBase);
    info = i;
    token = null;
    me = null;
    _api = _makeApi(entry.apiBase);
    await _storage.write(key: _kSchool, value: jsonEncode(school!.toJson()));
    await _storage.write(key: _kInfo, value: jsonEncode(i.toJson()));
    await _storage.delete(key: _kToken);
    notifyListeners();
  }

  Future<void> signIn(String mode, String loginId, String password) async {
    final res = await api.post('/app/auth/login', {'mode': mode, 'loginId': loginId.trim(), 'password': password});
    token = res['token'];
    api.token = token;
    mustChangePassword = res['mustChangePassword'] == true;
    me = Me.fromJson(res['me']);
    signedOutReason = null;
    await _storage.write(key: _kToken, value: token);
    await _storage.write(key: _kMust, value: mustChangePassword ? '1' : '0');
    notifyListeners();
    onMeChanged?.call(me);
  }

  Future<void> changePassword(String oldPassword, String newPassword) async {
    await api.post('/app/auth/change-password', {'oldPassword': oldPassword, 'newPassword': newPassword});
    mustChangePassword = false;
    await _storage.write(key: _kMust, value: '0');
    notifyListeners();
  }

  Future<void> refreshMe() async {
    me = Me.fromJson(await api.get('/app/me'));
    await refreshInfo(notify: false);
    notifyListeners();
    onMeChanged?.call(me);
  }

  /// The school's name, phone and the app versions on offer (update check).
  Future<void> refreshInfo({bool notify = true}) async {
    if (_api == null) return;
    try {
      info = SchoolInfo.fromJson(await api.get('/app/school-info'));
      await _storage.write(key: _kInfo, value: jsonEncode(info!.toJson()));
      if (notify) notifyListeners();
    } catch (_) {}
  }

  /// [beforeSignOut] runs while the token still works (removes this phone's push registration).
  Future<void> signOut({String? reason, Future<void> Function()? beforeSignOut}) async {
    if (token == null) return;
    if (beforeSignOut != null) {
      try {
        await beforeSignOut();
      } catch (_) {}
    }
    token = null;
    me = null;
    mustChangePassword = false;
    signedOutReason = reason;
    _api?.token = null;
    await _storage.delete(key: _kToken);
    notifyListeners();
    onMeChanged?.call(null);
  }

  Future<void> forgetSchool() async {
    await signOut();
    school = null;
    info = null;
    _api = null;
    await _storage.deleteAll();
    notifyListeners();
  }
}
