import 'dart:convert';

import 'package:http/http.dart' as http;

/// One EasySchool app serves every school: on first launch the user types
/// the School Code printed on their login slip, and it is looked up here to
/// find that school's server.
class SchoolEntry {
  const SchoolEntry({required this.code, required this.name, required this.apiBase});

  SchoolEntry.fromJson(Map<String, dynamic> j)
      : code = (j['code'] as String).toUpperCase(),
        name = j['name'] ?? '',
        apiBase = _clean(j['apiBase'] as String);

  final String code;
  final String name;

  /// https://school.example.com/api
  final String apiBase;

  Map<String, dynamic> toJson() => {'code': code, 'name': name, 'apiBase': apiBase};

  static String _clean(String url) {
    var u = url.trim();
    while (u.endsWith('/')) {
      u = u.substring(0, u.length - 1);
    }
    if (!u.startsWith('http://') && !u.startsWith('https://')) u = 'https://$u';
    if (!u.toLowerCase().endsWith('/api')) u = '$u/api';
    return u;
  }

  /// A server typed by hand (Settings / "School not listed?").
  factory SchoolEntry.manual(String url) => SchoolEntry(code: '', name: '', apiBase: _clean(url));
}

class SchoolDirectory {
  /// The live list, kept by EasySol, so a new school needs no app update.
  /// Empty: only the list below is used.
  static const remoteUrl = '';

  /// Built into the app; used when the live list cannot be read.
  /// A new school: add a line here (and a new app build), or set remoteUrl.
  static const builtIn = <SchoolEntry>[
    SchoolEntry(code: 'SSMHS', name: 'Sir Syed Model High School (Boys & Girls)', apiBase: 'https://school.easypos.com.pk/api'),
  ];

  static Future<SchoolEntry?> find(String code) async {
    final c = code.trim().toUpperCase();
    // Test builds: flutter run --dart-define=ES_API=http://10.0.2.2:5266/api
    const testApi = String.fromEnvironment('ES_API');
    if (testApi.isNotEmpty) return SchoolEntry(code: c, name: '', apiBase: SchoolEntry._clean(testApi));
    if (remoteUrl.isNotEmpty) {
      try {
        final res = await http.get(Uri.parse(remoteUrl)).timeout(const Duration(seconds: 10));
        if (res.statusCode == 200) {
          final list = (jsonDecode(res.body) as List).map((e) => SchoolEntry.fromJson(e as Map<String, dynamic>));
          for (final s in list) {
            if (s.code == c) return s;
          }
        }
      } catch (_) {}
    }
    for (final s in builtIn) {
      if (s.code == c) return s;
    }
    return null;
  }
}
