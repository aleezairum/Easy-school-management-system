import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api.dart';
import '../core/format.dart';
import '../core/models.dart';
import '../core/notice_watch.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../ui/widgets.dart';

Future<List<Announcement>> _loadAnnouncements(BuildContext context) async {
  final list = ((await context.read<Session>().api.get('/app/announcements')) as List).map((e) => Announcement.fromJson(e)).toList();
  // Shown in the app: the background check will not notify these again.
  NoticeWatch.markSeen(list.map((a) => a.id));
  return list;
}

class AnnouncementsTab extends StatefulWidget {
  const AnnouncementsTab({super.key});

  @override
  State<AnnouncementsTab> createState() => _AnnouncementsTabState();
}

class _AnnouncementsTabState extends State<AnnouncementsTab> {
  Key _key = UniqueKey();

  @override
  Widget build(BuildContext context) {
    final me = context.watch<Session>().me;
    if (me == null) return const SizedBox.shrink(); // signing out
    final canSend = me.isStaff && me.can(Feature.announceSend);
    return Scaffold(
      appBar: AppBar(title: const Text('School notices')),
      floatingActionButton: canSend
          ? FloatingActionButton.extended(
              onPressed: () async {
                final sent = await Navigator.push<bool>(context, MaterialPageRoute(builder: (_) => const PostAnnouncementScreen()));
                if (sent == true) setState(() => _key = UniqueKey());
              },
              icon: const Icon(Icons.add_rounded),
              label: const Text('New notice'),
            )
          : null,
      body: AsyncView<List<Announcement>>(
        key: _key,
        load: () => _loadAnnouncements(context),
        builder: (context, list, _) => PageList(children: [
          if (list.isEmpty)
            const EmptyView(icon: Icons.campaign_rounded, title: 'No notices right now', subtitle: 'School announcements will appear here, and on your phone as a notification.')
          else
            for (final a in list) Padding(padding: const EdgeInsets.only(bottom: 12), child: AnnouncementCard(a: a)),
        ]),
      ),
    );
  }
}

class AnnouncementCard extends StatelessWidget {
  const AnnouncementCard({super.key, required this.a, this.compact = false});
  final Announcement a;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final isNew = DateTime.now().difference(a.startDate).inDays < 3;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => AnnouncementScreen(a: a))),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          if (a.hasImage && !compact) AuthImage('/app/announcements/${a.id}/image', height: 170),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Container(
                padding: const EdgeInsets.all(9),
                decoration: BoxDecoration(color: AppColors.brand.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(12)),
                child: const Icon(Icons.campaign_rounded, color: AppColors.brand, size: 22),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Expanded(child: Text(a.title, style: t.titleSmall?.copyWith(fontWeight: FontWeight.w700))),
                    if (isNew) const Pill('New', color: AppColors.absent),
                  ]),
                  const SizedBox(height: 4),
                  Text(a.message, maxLines: compact ? 2 : 4, overflow: TextOverflow.ellipsis, style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                  const SizedBox(height: 8),
                  Text(d(a.startDate), style: t.labelSmall?.copyWith(color: cs.outline)),
                ]),
              ),
            ]),
          ),
        ]),
      ),
    );
  }
}

/// The newest notice, on top of the home screen.
class LatestNotice extends StatelessWidget {
  const LatestNotice({super.key});

  @override
  Widget build(BuildContext context) => FutureBuilder<List<Announcement>>(
        future: _loadAnnouncements(context),
        builder: (context, s) {
          final list = s.data;
          if (list == null || list.isEmpty) return const SizedBox.shrink();
          return Padding(padding: const EdgeInsets.only(bottom: 8), child: AnnouncementCard(a: list.first, compact: true));
        },
      );
}

class AnnouncementScreen extends StatelessWidget {
  const AnnouncementScreen({super.key, required this.a});
  final Announcement a;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Notice')),
      body: ListView(padding: const EdgeInsets.fromLTRB(20, 8, 20, 32), children: [
        Text(a.title, style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
        const SizedBox(height: 8),
        Row(children: [
          Icon(Icons.event_rounded, size: 16, color: cs.outline),
          const SizedBox(width: 6),
          Text(a.endDate == null ? d(a.startDate) : '${d(a.startDate)} - ${d(a.endDate!)}', style: t.bodySmall?.copyWith(color: cs.outline)),
        ]),
        const SizedBox(height: 20),
        if (a.hasImage) ...[
          ClipRRect(borderRadius: BorderRadius.circular(16), child: AuthImage('/app/announcements/${a.id}/image', height: 240)),
          const SizedBox(height: 20),
        ],
        SelectableText(a.message, style: t.bodyLarge?.copyWith(height: 1.5)),
      ]),
    );
  }
}

/// Opened from a notification: finds the announcement, then shows it.
class AnnouncementLoader extends StatelessWidget {
  const AnnouncementLoader({super.key, required this.id});
  final int id;

  @override
  Widget build(BuildContext context) => FutureBuilder<List<Announcement>>(
        future: _loadAnnouncements(context),
        builder: (context, s) {
          if (s.hasData) {
            final a = s.data!.where((x) => x.id == id).firstOrNull;
            if (a != null) return AnnouncementScreen(a: a);
            return Scaffold(appBar: AppBar(), body: const EmptyView(icon: Icons.campaign_rounded, title: 'This notice has ended'));
          }
          return Scaffold(
            appBar: AppBar(),
            body: s.hasError ? ErrorView(message: s.error.toString()) : const Center(child: CircularProgressIndicator()),
          );
        },
      );
}

/// Staff with "Send announcements": a notice for parents, staff or both, pushed at once.
class PostAnnouncementScreen extends StatefulWidget {
  const PostAnnouncementScreen({super.key});

  @override
  State<PostAnnouncementScreen> createState() => _PostAnnouncementScreenState();
}

class _PostAnnouncementScreenState extends State<PostAnnouncementScreen> {
  final _title = TextEditingController(), _message = TextEditingController();
  int _audience = 1;
  DateTime? _until;
  bool _busy = false;

  Future<void> _send() async {
    if (_title.text.trim().isEmpty || _message.text.trim().isEmpty) {
      showMessage(context, 'Write a title and the notice.', error: true);
      return;
    }
    setState(() => _busy = true);
    try {
      await context.read<Session>().api.post('/app/announcements', {
        'title': _title.text.trim(),
        'message': _message.text.trim(),
        'audience': _audience,
        if (_until != null) 'endDate': ymd(_until!),
      });
      if (!mounted) return;
      showMessage(context, 'Notice published. Phones are being notified.');
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final me = context.read<Session>().me!;
    return Scaffold(
      appBar: AppBar(title: const Text('New notice')),
      body: ListView(padding: const EdgeInsets.all(20), children: [
        Text('Who is it for?', style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: 8),
        SegmentedButton<int>(
          segments: const [
            ButtonSegment(value: 1, label: Text('Parents')),
            ButtonSegment(value: 2, label: Text('Staff')),
            ButtonSegment(value: 0, label: Text('Everyone')),
          ],
          selected: {_audience},
          onSelectionChanged: (v) => setState(() => _audience = v.first),
        ),
        if (!me.isAdmin && me.staff?.campusName != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text('Goes to ${me.staff!.campusName} only.', style: Theme.of(context).textTheme.bodySmall),
          ),
        const SizedBox(height: 20),
        TextField(controller: _title, maxLength: 200, decoration: const InputDecoration(labelText: 'Title')),
        const SizedBox(height: 8),
        TextField(controller: _message, maxLines: 7, maxLength: 3500, decoration: const InputDecoration(labelText: 'Notice', alignLabelWithHint: true)),
        const SizedBox(height: 8),
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.event_busy_rounded),
          title: Text(_until == null ? 'Show until switched off' : 'Show until ${d(_until!)}'),
          trailing: TextButton(
            child: const Text('Change'),
            onPressed: () async {
              final now = DateTime.now();
              final v = await showDatePicker(context: context, firstDate: now, lastDate: now.add(const Duration(days: 365)), initialDate: _until ?? now.add(const Duration(days: 7)));
              setState(() => _until = v);
            },
          ),
        ),
        const SizedBox(height: 20),
        FilledButton.icon(
          onPressed: _busy ? null : _send,
          icon: _busy ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white)) : const Icon(Icons.send_rounded),
          label: const Text('Publish and notify'),
        ),
      ]),
    );
  }
}
