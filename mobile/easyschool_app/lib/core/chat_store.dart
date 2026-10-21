import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';

/// One row of the WhatsApp-style list menu.
class ChatMenuRow {
  ChatMenuRow({required this.id, required this.title, this.description});
  ChatMenuRow.fromJson(Map<String, dynamic> j) : id = j['id'], title = j['title'] ?? '', description = j['description'];
  final String id, title;
  final String? description;
  Map<String, dynamic> toJson() => {'id': id, 'title': title, 'description': description};
}

class ChatMenu {
  ChatMenu.fromJson(Map<String, dynamic> j)
      : header = j['header'],
        footer = j['footer'],
        buttonText = j['buttonText'] ?? 'Menu',
        rows = ((j['rows'] as List?) ?? []).map((e) => ChatMenuRow.fromJson(e as Map<String, dynamic>)).toList();
  final String? header, footer;
  final String buttonText;
  final List<ChatMenuRow> rows;
  Map<String, dynamic> toJson() => {'header': header, 'footer': footer, 'buttonText': buttonText, 'rows': rows.map((r) => r.toJson()).toList()};
}

class ChatFile {
  ChatFile.fromJson(Map<String, dynamic> j)
      : id = j['id'],
        name = j['name'] ?? 'file',
        kind = j['kind'] ?? 'pdf',
        size = j['size'] ?? 0,
        path = j['path'];
  final String id, name, kind;
  final int size;

  /// Where the phone keeps its copy (null until downloaded; always null on the web preview).
  String? path;

  /// Kept in memory as well, so it shows at once and works on the web preview.
  Uint8List? bytes;
  bool failed = false;

  bool get isImage => kind == 'image';
  Map<String, dynamic> toJson() => {'id': id, 'name': name, 'kind': kind, 'size': size, 'path': path};
}

class ChatMsg {
  ChatMsg({required this.mine, required this.text, DateTime? at, this.menu, this.file, this.read = false})
      : at = at ?? DateTime.now(),
        id = '${DateTime.now().microsecondsSinceEpoch}${_seq++}';

  ChatMsg.fromJson(Map<String, dynamic> j)
      : id = j['id'],
        mine = j['mine'] ?? false,
        text = j['text'] ?? '',
        at = DateTime.parse(j['at']),
        read = j['read'] ?? true,
        menu = j['menu'] == null ? null : ChatMenu.fromJson(j['menu']),
        file = j['file'] == null ? null : ChatFile.fromJson(j['file']);

  static int _seq = 0;
  final String id;
  final bool mine;
  final String text;
  final DateTime at;
  final ChatMenu? menu;
  final ChatFile? file;

  /// Mine: the school has answered (blue ticks).
  bool read;

  Map<String, dynamic> toJson() => {
        'id': id, 'mine': mine, 'text': text, 'at': at.toIso8601String(), 'read': read,
        if (menu != null) 'menu': menu!.toJson(),
        if (file != null) 'file': file!.toJson(),
      };
}

/// The conversation is kept on the phone (like WhatsApp), one file per login
/// and mode; PDFs and pictures beside it, so they open again later.
class ChatStore {
  ChatStore(this.key);
  final String key;
  static const _keep = 300;

  Future<Directory?> _dir() async {
    if (kIsWeb) return null;
    final d = Directory('${(await getApplicationDocumentsDirectory()).path}/chat');
    if (!await d.exists()) await d.create(recursive: true);
    return d;
  }

  Future<List<ChatMsg>> load() async {
    try {
      final d = await _dir();
      if (d == null) return [];
      final f = File('${d.path}/chat_$key.json');
      if (!await f.exists()) return [];
      final list = (jsonDecode(await f.readAsString()) as List).map((e) => ChatMsg.fromJson(e as Map<String, dynamic>)).toList();
      return list;
    } catch (_) {
      return [];
    }
  }

  Future<void> save(List<ChatMsg> msgs) async {
    try {
      final d = await _dir();
      if (d == null) return;
      final keep = msgs.length > _keep ? msgs.sublist(msgs.length - _keep) : msgs;
      await File('${d.path}/chat_$key.json').writeAsString(jsonEncode(keep.map((m) => m.toJson()).toList()));
    } catch (_) {}
  }

  /// Saves a downloaded PDF / picture; returns its path (null on the web preview).
  Future<String?> saveFile(ChatFile f, Uint8List bytes) async {
    final d = await _dir();
    if (d == null) return null;
    final files = Directory('${d.path}/files');
    if (!await files.exists()) await files.create(recursive: true);
    final safe = f.name.replaceAll(RegExp(r'[^A-Za-z0-9._-]'), '_');
    final out = File('${files.path}/${f.id}_$safe');
    await out.writeAsBytes(bytes);
    return out.path;
  }

  Future<Uint8List?> readFile(ChatFile f) async {
    if (f.bytes != null) return f.bytes;
    if (f.path == null || kIsWeb) return null;
    final file = File(f.path!);
    return await file.exists() ? f.bytes = await file.readAsBytes() : null;
  }

  Future<void> clear(List<ChatMsg> msgs) async {
    try {
      for (final m in msgs) {
        if (m.file?.path != null) {
          final f = File(m.file!.path!);
          if (await f.exists()) await f.delete();
        }
      }
    } catch (_) {}
    await save([]);
  }
}
