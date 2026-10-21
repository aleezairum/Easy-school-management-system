import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/api.dart';
import '../core/chat_store.dart';
import '../core/session.dart';
import '../core/theme.dart';
import 'pdf_screen.dart';

/// The school's WhatsApp self-service menu, inside the app: say "hi", pick
/// from the menu, get the same answers and PDFs as on WhatsApp
/// (POST api/app/chat - composed by the WhatsApp bot itself). Free: nothing
/// goes through Meta. The conversation stays on the phone, like WhatsApp.
class ChatTab extends StatefulWidget {
  const ChatTab({super.key});

  @override
  State<ChatTab> createState() => _ChatTabState();
}

// WhatsApp's light-theme colours.
const _wallpaper = Color(0xFFEFE7DE);
const _mineBubble = Color(0xFFD9FDD3);
const _blueTick = Color(0xFF53BDEB);
const _meta = Color(0xFF667781);
const _link = Color(0xFF027EB5);

class _ChatTabState extends State<ChatTab> {
  final _input = TextEditingController();
  final _scroll = ScrollController();
  final _msgs = <ChatMsg>[];
  late ChatStore _store;
  bool _loaded = false, _typing = false, _sending = false;

  @override
  void initState() {
    super.initState();
    final me = context.read<Session>().me!;
    _store = ChatStore('${me.accountId}_${me.mode}');
    _store.load().then((list) {
      if (!mounted) return;
      setState(() {
        _msgs.addAll(list);
        _loaded = true;
      });
    });
  }

  @override
  void dispose() {
    _input.dispose();
    _scroll.dispose();
    super.dispose();
  }

  /// [shown] is what the bubble says (a menu row's title); [text] what goes to the server (its id).
  Future<void> _send(String text, {String? shown}) async {
    text = text.trim();
    if (text.isEmpty || _sending) return;
    final api = context.read<Session>().api;
    final mine = ChatMsg(mine: true, text: shown ?? text);
    setState(() {
      _msgs.add(mine);
      _typing = true;
      _sending = true;
    });
    _input.clear();
    try {
      final replies = (await api.post('/app/chat', {'text': text}) as List).cast<Map<String, dynamic>>();
      mine.read = true;
      for (final r in replies) {
        await Future.delayed(const Duration(milliseconds: 350));
        if (!mounted) return;
        final m = ChatMsg(
          mine: false,
          text: r['text'] ?? '',
          menu: r['menu'] == null ? null : ChatMenu.fromJson(r['menu']),
          file: r['file'] == null ? null : ChatFile.fromJson(r['file']),
        );
        setState(() => _msgs.add(m));
        if (m.file != null) _download(m.file!);
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _msgs.add(ChatMsg(mine: false, text: '⚠️ ${e.message}\n_Your message was not delivered. Please try again._')));
      }
    } finally {
      if (mounted) {
        setState(() {
          _typing = false;
          _sending = false;
        });
      }
      _store.save(_msgs);
    }
  }

  Future<void> _download(ChatFile f) async {
    try {
      final bytes = await context.read<Session>().api.bytes('/app/chat/files/${f.id}');
      f.bytes = bytes;
      f.path = await _store.saveFile(f, bytes);
    } catch (_) {
      f.failed = true;
    }
    if (mounted) setState(() {});
    _store.save(_msgs);
  }

  Future<void> _openFile(ChatFile f) async {
    final bytes = await _store.readFile(f);
    if (!mounted) return;
    if (bytes == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('This file is no longer available. Ask again in the chat.')));
      return;
    }
    if (f.isImage) {
      Navigator.push(context, MaterialPageRoute(builder: (_) => _Picture(bytes: bytes, title: f.name)));
    } else {
      PdfScreen.openBytes(context, title: f.name.replaceAll('_', ' ').replaceAll('.pdf', ''), data: bytes, fileName: f.name);
    }
  }

  Future<void> _showMenu(ChatMenu menu) async {
    final row = await showModalBottomSheet<ChatMenuRow>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: Colors.white,
      builder: (_) => _MenuSheet(menu: menu),
    );
    if (row != null) _send(row.id, shown: row.title);
  }

  Future<void> _clear() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Clear this chat?'),
        content: const Text('Messages and the documents saved with them are removed from this phone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Clear chat')),
        ],
      ),
    );
    if (ok != true) return;
    await _store.clear(_msgs);
    setState(() => _msgs.clear());
  }

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final school = s.info?.name ?? 'School';
    final items = _items();
    return Scaffold(
      backgroundColor: _wallpaper,
      appBar: AppBar(
        backgroundColor: AppColors.brand,
        foregroundColor: Colors.white,
        titleSpacing: 12,
        title: Row(children: [
          Container(
            width: 40,
            height: 40,
            decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
            child: const Icon(Icons.school_rounded, color: AppColors.brand, size: 24),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(school, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white)),
              Text(_typing ? 'typing…' : 'School office · replies at once',
                  style: TextStyle(fontSize: 12.5, color: Colors.white.withValues(alpha: 0.85), fontWeight: FontWeight.w400)),
            ]),
          ),
        ]),
        actions: [
          PopupMenuButton<String>(
            onSelected: (v) => v == 'clear' ? _clear() : null,
            itemBuilder: (_) => const [PopupMenuItem(value: 'clear', child: Text('Clear chat'))],
          ),
        ],
      ),
      body: Column(children: [
        Expanded(
          child: !_loaded
              ? const Center(child: CircularProgressIndicator())
              : _msgs.isEmpty
                  ? _Welcome(onHi: () => _send('hi', shown: 'Hi'))
                  : ListView.builder(
                      controller: _scroll,
                      reverse: true,
                      padding: const EdgeInsets.fromLTRB(10, 10, 10, 6),
                      itemCount: items.length,
                      itemBuilder: (context, i) => items[items.length - 1 - i],
                    ),
        ),
        _InputBar(controller: _input, sending: _sending, onSend: () => _send(_input.text)),
      ]),
    );
  }

  /// Bubbles in order, with a date chip whenever the day changes, and "typing…" last.
  List<Widget> _items() {
    final out = <Widget>[];
    DateTime? day;
    for (var i = 0; i < _msgs.length; i++) {
      final m = _msgs[i];
      final d = DateTime(m.at.year, m.at.month, m.at.day);
      if (day != d) {
        out.add(_DateChip(d));
        day = d;
      }
      final firstOfRun = i == 0 || _msgs[i - 1].mine != m.mine || DateTime(_msgs[i - 1].at.year, _msgs[i - 1].at.month, _msgs[i - 1].at.day) != d;
      out.add(_Bubble(msg: m, tail: firstOfRun, onMenu: _showMenu, onFile: _openFile));
    }
    if (_typing) out.add(const _Typing());
    return out;
  }
}

// ── pieces ─────────────────────────────────────────────────────────────

class _Welcome extends StatelessWidget {
  const _Welcome({required this.onHi});
  final VoidCallback onHi;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(color: const Color(0xFFFFF5C4), borderRadius: BorderRadius.circular(8)),
              child: const Text(
                '🔒 This chat is between you and the school office. Fees, results, attendance, diary and notices - ask any time.',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 12.5, color: Color(0xFF54656F)),
              ),
            ),
            const SizedBox(height: 28),
            const Text('👋', style: TextStyle(fontSize: 54)),
            const SizedBox(height: 10),
            const Text('Say Hi to see the menu', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
            const SizedBox(height: 6),
            const Text('The same menu as the school\'s WhatsApp,\nright here in the app.', textAlign: TextAlign.center, style: TextStyle(color: _meta)),
            const SizedBox(height: 20),
            FilledButton.icon(
              style: FilledButton.styleFrom(minimumSize: const Size(160, 48), shape: const StadiumBorder()),
              onPressed: onHi,
              icon: const Icon(Icons.waving_hand_rounded),
              label: const Text('Hi'),
            ),
          ]),
        ),
      );
}

class _DateChip extends StatelessWidget {
  const _DateChip(this.day);
  final DateTime day;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final label = day == today
        ? 'Today'
        : day == today.subtract(const Duration(days: 1))
            ? 'Yesterday'
            : DateFormat('d MMMM yyyy').format(day);
    return Center(
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 8),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
        decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(8), boxShadow: const [BoxShadow(color: Color(0x14000000), blurRadius: 1, offset: Offset(0, 1))]),
        child: Text(label, style: const TextStyle(fontSize: 12.5, color: Color(0xFF54656F))),
      ),
    );
  }
}

class _Bubble extends StatelessWidget {
  const _Bubble({required this.msg, required this.tail, required this.onMenu, required this.onFile});
  final ChatMsg msg;
  final bool tail;
  final void Function(ChatMenu) onMenu;
  final void Function(ChatFile) onFile;

  @override
  Widget build(BuildContext context) {
    final mine = msg.mine;
    final maxW = MediaQuery.of(context).size.width * 0.82;
    final time = DateFormat('h:mm a').format(msg.at).toLowerCase();
    final meta = Row(mainAxisSize: MainAxisSize.min, children: [
      Text(time, style: const TextStyle(fontSize: 11, color: _meta)),
      if (mine) ...[
        const SizedBox(width: 3),
        Icon(msg.read ? Icons.done_all_rounded : Icons.done_rounded, size: 16, color: msg.read ? _blueTick : _meta),
      ],
    ]);

    final children = <Widget>[];
    final f = msg.file;
    if (f != null) children.add(f.isImage ? _ImageBox(file: f, onTap: () => onFile(f)) : _DocBox(file: f, onTap: () => onFile(f)));
    if (msg.text.isNotEmpty) {
      children.add(Padding(
        padding: EdgeInsets.fromLTRB(4, f != null ? 6 : 2, 4, 0),
        child: WaText(msg.text),
      ));
    }
    if (msg.menu?.footer != null && msg.menu!.footer!.isNotEmpty) {
      children.add(Padding(
        padding: const EdgeInsets.fromLTRB(4, 4, 4, 0),
        child: Text(msg.menu!.footer!, style: const TextStyle(fontSize: 12.5, color: _meta)),
      ));
    }
    children.add(Align(alignment: Alignment.bottomRight, child: Padding(padding: const EdgeInsets.only(top: 2), child: meta)));

    final bubble = Container(
      constraints: BoxConstraints(maxWidth: maxW),
      margin: EdgeInsets.only(top: tail ? 6 : 2),
      decoration: BoxDecoration(
        color: mine ? _mineBubble : Colors.white,
        borderRadius: BorderRadius.only(
          topLeft: Radius.circular(!mine && tail ? 0 : 10),
          topRight: Radius.circular(mine && tail ? 0 : 10),
          bottomLeft: const Radius.circular(10),
          bottomRight: const Radius.circular(10),
        ),
        boxShadow: const [BoxShadow(color: Color(0x1A000000), blurRadius: 1, offset: Offset(0, 1))],
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
        Padding(padding: const EdgeInsets.fromLTRB(6, 5, 6, 4), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children)),
        if (msg.menu != null) ...[
          const Divider(height: 1, thickness: 0.6, color: Color(0xFFE2E2E2)),
          InkWell(
            onTap: () => onMenu(msg.menu!),
            borderRadius: const BorderRadius.vertical(bottom: Radius.circular(10)),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 11),
              child: Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                const Icon(Icons.format_list_bulleted_rounded, size: 19, color: _link),
                const SizedBox(width: 8),
                Text(msg.menu!.buttonText, style: const TextStyle(color: _link, fontWeight: FontWeight.w600, fontSize: 15)),
              ]),
            ),
          ),
        ],
      ]),
    );
    return Align(alignment: mine ? Alignment.centerRight : Alignment.centerLeft, child: bubble);
  }
}

class _DocBox extends StatelessWidget {
  const _DocBox({required this.file, required this.onTap});
  final ChatFile file;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final ready = file.bytes != null || file.path != null;
    final kb = file.size / 1024;
    final size = kb >= 1024 ? '${(kb / 1024).toStringAsFixed(1)} MB' : '${kb.round()} kB';
    return InkWell(
      onTap: ready ? onTap : null,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 270,
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(color: const Color(0xFFF5F6F6), borderRadius: BorderRadius.circular(8)),
        child: Row(children: [
          Container(
            width: 36,
            height: 44,
            alignment: Alignment.bottomCenter,
            padding: const EdgeInsets.only(bottom: 5),
            decoration: BoxDecoration(color: const Color(0xFFE5483B), borderRadius: BorderRadius.circular(4)),
            child: const Text('PDF', style: TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w800)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(file.name, maxLines: 2, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 14, color: Color(0xFF111B21))),
              const SizedBox(height: 3),
              Text('PDF · $size', style: const TextStyle(fontSize: 12, color: _meta)),
            ]),
          ),
          const SizedBox(width: 6),
          if (file.failed)
            const Icon(Icons.error_outline_rounded, color: AppColors.absent)
          else if (!ready)
            const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.2))
          else
            const Icon(Icons.open_in_new_rounded, color: _meta, size: 20),
        ]),
      ),
    );
  }
}

class _ImageBox extends StatelessWidget {
  const _ImageBox({required this.file, required this.onTap});
  final ChatFile file;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final bytes = file.bytes;
    return GestureDetector(
      onTap: onTap,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: SizedBox(
          width: 260,
          child: bytes != null
              ? Image.memory(bytes, fit: BoxFit.cover)
              : file.path != null
                  ? FutureBuilder<Uint8List?>(
                      future: ChatStore('').readFile(file),
                      builder: (_, s) => s.data != null ? Image.memory(s.data!, fit: BoxFit.cover) : const _ImagePlaceholder(),
                    )
                  : _ImagePlaceholder(failed: file.failed),
        ),
      ),
    );
  }
}

class _ImagePlaceholder extends StatelessWidget {
  const _ImagePlaceholder({this.failed = false});
  final bool failed;

  @override
  Widget build(BuildContext context) => Container(
        height: 180,
        color: const Color(0xFFE9EDEF),
        alignment: Alignment.center,
        child: failed ? const Icon(Icons.broken_image_outlined, color: _meta) : const CircularProgressIndicator(strokeWidth: 2.2),
      );
}

class _Picture extends StatelessWidget {
  const _Picture({required this.bytes, required this.title});
  final Uint8List bytes;
  final String title;

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: Colors.black,
        appBar: AppBar(backgroundColor: Colors.black, foregroundColor: Colors.white, title: Text(title, style: const TextStyle(color: Colors.white, fontSize: 16))),
        body: Center(child: InteractiveViewer(maxScale: 5, child: Image.memory(bytes))),
      );
}

class _Typing extends StatefulWidget {
  const _Typing();

  @override
  State<_Typing> createState() => _TypingState();
}

class _TypingState extends State<_Typing> with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(vsync: this, duration: const Duration(milliseconds: 1100))..repeat();

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Align(
        alignment: Alignment.centerLeft,
        child: Container(
          margin: const EdgeInsets.only(top: 6),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.only(topRight: Radius.circular(10), bottomLeft: Radius.circular(10), bottomRight: Radius.circular(10)),
          ),
          child: AnimatedBuilder(
            animation: _c,
            builder: (_, _) => Row(mainAxisSize: MainAxisSize.min, children: [
              for (var i = 0; i < 3; i++)
                Container(
                  margin: const EdgeInsets.symmetric(horizontal: 2),
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: _meta.withValues(alpha: 0.35 + 0.65 * (((_c.value * 3 - i) % 3) < 1 ? 1 - ((_c.value * 3 - i) % 3) : 0)),
                    shape: BoxShape.circle,
                  ),
                ),
            ]),
          ),
        ),
      );
}

class _InputBar extends StatelessWidget {
  const _InputBar({required this.controller, required this.sending, required this.onSend});
  final TextEditingController controller;
  final bool sending;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) => SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(8, 4, 8, 8),
          child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
            Expanded(
              child: Container(
                decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(26), boxShadow: const [BoxShadow(color: Color(0x14000000), blurRadius: 1, offset: Offset(0, 1))]),
                child: TextField(
                  controller: controller,
                  minLines: 1,
                  maxLines: 5,
                  textCapitalization: TextCapitalization.sentences,
                  textInputAction: TextInputAction.send,
                  onSubmitted: (_) => onSend(),
                  decoration: const InputDecoration(
                    hintText: 'Message',
                    filled: false,
                    border: InputBorder.none,
                    enabledBorder: InputBorder.none,
                    focusedBorder: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 18, vertical: 12),
                  ),
                ),
              ),
            ),
            const SizedBox(width: 6),
            SizedBox(
              width: 48,
              height: 48,
              child: FloatingActionButton(
                heroTag: null,
                elevation: 1,
                backgroundColor: AppColors.brand,
                foregroundColor: Colors.white,
                shape: const CircleBorder(),
                onPressed: sending ? null : onSend,
                child: const Icon(Icons.send_rounded, size: 22),
              ),
            ),
          ]),
        ),
      );
}

/// WhatsApp's list-menu sheet: pick one row, then Send.
class _MenuSheet extends StatefulWidget {
  const _MenuSheet({required this.menu});
  final ChatMenu menu;

  @override
  State<_MenuSheet> createState() => _MenuSheetState();
}

class _MenuSheetState extends State<_MenuSheet> {
  ChatMenuRow? _pick;

  @override
  Widget build(BuildContext context) {
    final rows = widget.menu.rows;
    return SafeArea(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * 0.85),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(8, 0, 16, 4),
            child: Row(children: [
              IconButton(icon: const Icon(Icons.close_rounded), onPressed: () => Navigator.pop(context)),
              Expanded(child: Text(widget.menu.buttonText, textAlign: TextAlign.center, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600))),
              const SizedBox(width: 40),
            ]),
          ),
          const Divider(height: 1),
          Flexible(
            child: RadioGroup<String>(
              groupValue: _pick?.id,
              onChanged: (v) => setState(() => _pick = rows.firstWhere((r) => r.id == v)),
              child: ListView(shrinkWrap: true, children: [
                for (final r in rows)
                  RadioListTile<String>(
                    value: r.id,
                    controlAffinity: ListTileControlAffinity.trailing,
                    activeColor: AppColors.brand,
                    title: Text(r.title, style: const TextStyle(fontSize: 15.5, color: Color(0xFF111B21))),
                    subtitle: r.description == null ? null : Text(r.description!, style: const TextStyle(fontSize: 13, color: _meta)),
                  ),
              ]),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: FilledButton(
              style: FilledButton.styleFrom(shape: const StadiumBorder()),
              onPressed: _pick == null ? null : () => Navigator.pop(context, _pick),
              child: const Text('Send'),
            ),
          ),
        ]),
      ),
    );
  }
}

/// WhatsApp's formatting: *bold*, _italic_, ~strike~, ```mono```, and tappable links.
class WaText extends StatelessWidget {
  const WaText(this.text, {super.key});
  final String text;

  static final _token = RegExp(r'```([\s\S]+?)```|\*([^*\n]+)\*|(?<![A-Za-z0-9])_([^_\n]+)_(?![A-Za-z0-9])|~([^~\n]+)~|(https?://[^\s]+)');

  @override
  Widget build(BuildContext context) {
    const base = TextStyle(fontSize: 15, height: 1.35, color: Color(0xFF111B21));
    final spans = <InlineSpan>[];
    var at = 0;
    for (final m in _token.allMatches(text)) {
      if (m.start > at) spans.add(TextSpan(text: text.substring(at, m.start)));
      if (m.group(1) != null) {
        spans.add(TextSpan(text: m.group(1), style: const TextStyle(fontFamily: 'monospace')));
      } else if (m.group(2) != null) {
        spans.add(TextSpan(text: m.group(2), style: const TextStyle(fontWeight: FontWeight.w700)));
      } else if (m.group(3) != null) {
        spans.add(TextSpan(text: m.group(3), style: const TextStyle(fontStyle: FontStyle.italic)));
      } else if (m.group(4) != null) {
        spans.add(TextSpan(text: m.group(4), style: const TextStyle(decoration: TextDecoration.lineThrough)));
      } else {
        final url = m.group(5)!;
        spans.add(TextSpan(
          text: url,
          style: const TextStyle(color: _link, decoration: TextDecoration.underline),
          recognizer: TapGestureRecognizer()..onTap = () => launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication),
        ));
      }
      at = m.end;
    }
    if (at < text.length) spans.add(TextSpan(text: text.substring(at)));
    return SelectableText.rich(TextSpan(style: base, children: spans));
  }
}
