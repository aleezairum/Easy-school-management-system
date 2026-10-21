import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

/// A refusal or failure from the school's server, worded for the user.
class ApiException implements Exception {
  ApiException(this.status, this.message);

  /// HTTP status; 0 when the server could not be reached.
  final int status;
  final String message;

  @override
  String toString() => message;
}

/// The school's EasySchool API (…/api). Adds the sign-in token, turns errors
/// into [ApiException] with the server's own message, and reports a 401 so the
/// app can return to the sign-in screen.
class Api {
  Api(this.base, {this.token, this.onUnauthorized});

  /// e.g. https://school.example.com/api — no trailing slash.
  final String base;
  String? token;
  void Function()? onUnauthorized;

  static const _timeout = Duration(seconds: 30);

  Uri uri(String path, [Map<String, String>? query]) => Uri.parse('$base$path').replace(queryParameters: query);

  Map<String, String> get _headers => {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      };

  Future<dynamic> get(String path, {Map<String, String>? query}) =>
      _send(() => http.get(uri(path, query), headers: _headers));

  Future<dynamic> post(String path, [Object? body]) =>
      _send(() => http.post(uri(path), headers: _headers, body: jsonEncode(body ?? {})));

  Future<dynamic> delete(String path, {Map<String, String>? query}) =>
      _send(() => http.delete(uri(path, query), headers: _headers));

  /// A file (PDF or picture) as bytes.
  Future<Uint8List> bytes(String path) async {
    final res = await _raw(() => http.get(uri(path), headers: _headers));
    return res.bodyBytes;
  }

  Future<dynamic> _send(Future<http.Response> Function() call) async {
    final res = await _raw(call);
    if (res.body.isEmpty) return null;
    return jsonDecode(utf8.decode(res.bodyBytes));
  }

  Future<http.Response> _raw(Future<http.Response> Function() call) async {
    http.Response res;
    try {
      res = await call().timeout(_timeout);
    } on TimeoutException {
      throw ApiException(0, 'The school server is taking too long. Please try again.');
    } on http.ClientException {
      throw ApiException(0, 'Could not reach the school server. Check your internet connection and try again.');
    }
    if (res.statusCode >= 200 && res.statusCode < 300) return res;

    String? message;
    try {
      final j = jsonDecode(utf8.decode(res.bodyBytes));
      if (j is Map && j['message'] is String) message = j['message'] as String;
    } catch (_) {}
    if (res.statusCode == 401 && token != null) onUnauthorized?.call();
    throw ApiException(
      res.statusCode,
      message ??
          switch (res.statusCode) {
            401 => 'Please sign in again.',
            403 => 'You do not have access to this.',
            404 => 'Not available yet.',
            _ => 'Something went wrong on the school server (${res.statusCode}).',
          },
    );
  }
}
