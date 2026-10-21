import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/format.dart';
import '../core/models.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../ui/widgets.dart';
import 'announcements.dart';
import 'pdf_screen.dart';

// ── attendance colours ───────────────────────────────────────────────

const attendanceStyle = {
  'P': (AppColors.present, 'Present'),
  'T': (AppColors.late, 'Late'),
  'A': (AppColors.absent, 'Absent'),
  'L': (AppColors.leave, 'Leave'),
  'H': (AppColors.holiday, 'Holiday'),
  'S': (AppColors.off, 'Sunday'),
};

String todayText(String? s) => switch (s) {
      'P' => 'Present today',
      'T' => 'Late today',
      'A' => 'Absent today',
      'L' => 'On leave today',
      _ => 'Not marked yet today',
    };

Color todayColor(String? s) => attendanceStyle[s]?.$1 ?? AppColors.off;

// ── parent: the family ───────────────────────────────────────────────

class FamilyTab extends StatelessWidget {
  const FamilyTab({super.key});

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final me = s.me;
    if (me == null) return const SizedBox.shrink(); // signing out
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final parentName = me.students.firstOrNull?.fatherName;
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => s.refreshMe(),
          child: PageList(padding: const EdgeInsets.fromLTRB(16, 16, 16, 24), children: [
            Text(greeting(), style: t.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
            Text(parentName?.isNotEmpty == true ? parentName! : 'Parent', style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
            Text(s.info?.name ?? '', style: t.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
            const SizedBox(height: 16),
            const LatestNotice(),
            SectionTitle(me.students.length == 1 ? 'Your child' : 'Your children (${me.students.length})', top: 8),
            if (me.students.isEmpty)
              const EmptyView(icon: Icons.child_care_rounded, title: 'No children found', subtitle: 'Ask the school office to check your child\'s admission.')
            else
              for (final st in me.students) Padding(padding: const EdgeInsets.only(bottom: 12), child: ChildCard(student: st)),
          ]),
        ),
      ),
    );
  }
}

/// A child on the family screen, with this month's figures.
class ChildCard extends StatefulWidget {
  const ChildCard({super.key, required this.student});
  final StudentCard student;

  @override
  State<ChildCard> createState() => _ChildCardState();
}

class _ChildCardState extends State<ChildCard> {
  late Future<StudentSummary> _summary;

  @override
  void initState() {
    super.initState();
    _summary = _load();
  }

  Future<StudentSummary> _load() async =>
      StudentSummary.fromJson(await context.read<Session>().api.get('/app/students/${widget.student.studentId}/summary'));

  @override
  Widget build(BuildContext context) {
    final st = widget.student;
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => StudentHome(student: st))),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(children: [
            Row(children: [
              InitialsAvatar(st.name, size: 52, color: st.isGirl ? const Color(0xFFDB2777) : AppColors.leave),
              const SizedBox(width: 14),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(st.name, style: t.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  Text('${st.classLabel}  ·  Roll ${st.rollNo ?? '-'}', style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                  if (st.campusName != null) Text(st.campusName!, style: t.bodySmall?.copyWith(color: cs.outline)),
                ]),
              ),
              Icon(Icons.chevron_right_rounded, color: cs.outline),
            ]),
            const SizedBox(height: 14),
            FutureBuilder<StudentSummary>(
              future: _summary,
              builder: (context, snap) {
                final s = snap.data;
                if (s == null) {
                  return SizedBox(height: 58, child: Center(child: snap.hasError ? Text('Could not load figures', style: t.bodySmall) : const LinearProgressIndicator()));
                }
                return Column(children: [
                  Row(children: [
                    _MiniStat(label: 'Attendance', value: pct(s.monthPercent), color: AppColors.present),
                    _MiniStat(label: 'Fee due', value: s.duesTotal > 0 ? rs(s.duesTotal) : 'Paid', color: s.duesTotal > 0 ? AppColors.money : AppColors.present),
                    _MiniStat(label: 'Last result', value: pct(s.lastExamPercent), color: AppColors.leave),
                  ]),
                  const SizedBox(height: 10),
                  Align(alignment: Alignment.centerLeft, child: Pill(todayText(s.todayStatus), color: todayColor(s.todayStatus), icon: Icons.circle)),
                ]);
              },
            ),
          ]),
        ),
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  const _MiniStat({required this.label, required this.value, required this.color});
  final String label, value;
  final Color color;

  @override
  Widget build(BuildContext context) => Expanded(
        child: Container(
          margin: const EdgeInsets.symmetric(horizontal: 3),
          padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 6),
          decoration: BoxDecoration(color: color.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(12)),
          child: Column(children: [
            FittedBox(child: Text(value, style: TextStyle(color: color, fontWeight: FontWeight.w800, fontSize: 16))),
            const SizedBox(height: 2),
            Text(label, style: Theme.of(context).textTheme.labelSmall?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant)),
          ]),
        ),
      );
}

// ── one child ────────────────────────────────────────────────────────

/// A child's own dashboard (the Home tab in student mode).
class StudentHome extends StatelessWidget {
  const StudentHome({super.key, required this.student, this.asTab = false});
  final StudentCard student;
  final bool asTab;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final id = student.studentId;
    return Scaffold(
      appBar: asTab ? null : AppBar(title: Text(firstName(student.name))),
      body: SafeArea(
        top: asTab,
        child: AsyncView<(StudentSummary, Timing?)>(
          load: () async {
            final s = StudentSummary.fromJson(await api.get('/app/students/$id/summary'));
            Timing? tm;
            try {
              final j = await api.get('/app/students/$id/timing');
              if (j != null) tm = Timing.fromJson(j);
            } catch (_) {}
            return (s, tm);
          },
          builder: (context, data, reload) {
            final (s, timing) = data;
            void go(Widget page) => Navigator.push(context, MaterialPageRoute(builder: (_) => page));
            return PageList(children: [
              _StudentHeader(student: s.student, today: s.todayStatus),
              if (asTab) ...[const SizedBox(height: 12), const LatestNotice()],
              const SectionTitle('This month'),
              TileGrid(children: [
                StatTile(
                  icon: Icons.event_available_rounded,
                  color: AppColors.present,
                  label: 'Attendance',
                  value: pct(s.monthPercent),
                  sub: '${s.monthPresent} present · ${s.monthAbsent} absent',
                  onTap: () => go(AttendanceScreen(student: s.student)),
                ),
                StatTile(
                  icon: Icons.account_balance_wallet_rounded,
                  color: s.duesTotal > 0 ? AppColors.money : AppColors.present,
                  label: 'Fee due',
                  value: s.duesTotal > 0 ? rs(s.duesTotal) : 'All paid',
                  sub: s.duesTotal > 0 ? '${s.duesMonths} month${s.duesMonths == 1 ? '' : 's'}' : null,
                  onTap: () => go(FeesScreen(student: s.student)),
                ),
                StatTile(
                  icon: Icons.emoji_events_rounded,
                  color: AppColors.leave,
                  label: s.lastExamName ?? 'Results',
                  value: pct(s.lastExamPercent),
                  onTap: () => go(ResultsScreen(student: s.student)),
                ),
                StatTile(
                  icon: Icons.menu_book_rounded,
                  color: AppColors.holiday,
                  label: 'Diary today',
                  value: s.diaryPagesToday == 0 ? 'None' : '${s.diaryPagesToday} page${s.diaryPagesToday == 1 ? '' : 's'}',
                  onTap: () => go(DiaryScreen(student: s.student)),
                ),
              ]),
              const SectionTitle('Everything'),
              GroupCard(children: [
                MenuTile(icon: Icons.calendar_month_rounded, color: AppColors.present, title: 'Attendance', subtitle: 'Day by day, month by month', onTap: () => go(AttendanceScreen(student: s.student))),
                MenuTile(icon: Icons.receipt_long_rounded, color: AppColors.money, title: 'Fees', subtitle: 'Dues, fee voucher and ledger', onTap: () => go(FeesScreen(student: s.student))),
                MenuTile(icon: Icons.workspace_premium_rounded, color: AppColors.leave, title: 'Results', subtitle: 'Marks and result cards', onTap: () => go(ResultsScreen(student: s.student))),
                MenuTile(icon: Icons.auto_stories_rounded, color: AppColors.holiday, title: 'Diary', subtitle: 'Homework from the class diary', onTap: () => go(DiaryScreen(student: s.student))),
              ]),
              if (timing != null) ...[
                const SectionTitle('School timings'),
                _TimingCard(timing: timing),
              ],
            ]);
          },
        ),
      ),
    );
  }
}

class _StudentHeader extends StatelessWidget {
  const _StudentHeader({required this.student, required this.today});
  final StudentCard student;
  final String? today;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        gradient: const LinearGradient(colors: [AppColors.brand, AppColors.brandDark], begin: Alignment.topLeft, end: Alignment.bottomRight),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Container(
            width: 56,
            height: 56,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.18), shape: BoxShape.circle),
            child: Text(initials(student.name), style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w800)),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(student.name, style: t.titleLarge?.copyWith(color: Colors.white, fontWeight: FontWeight.w800)),
              Text(student.classLabel, style: t.bodyMedium?.copyWith(color: Colors.white.withValues(alpha: 0.9))),
            ]),
          ),
        ]),
        const SizedBox(height: 14),
        Wrap(spacing: 8, runSpacing: 8, children: [
          _HeaderChip(Icons.tag_rounded, 'Roll ${student.rollNo ?? '-'}'),
          if (student.campusName != null) _HeaderChip(Icons.location_on_outlined, student.campusName!),
          _HeaderChip(Icons.circle, todayText(today), dot: todayColor(today)),
        ]),
      ]),
    );
  }
}

class _HeaderChip extends StatelessWidget {
  const _HeaderChip(this.icon, this.text, {this.dot});
  final IconData icon;
  final String text;
  final Color? dot;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        decoration: BoxDecoration(color: Colors.white.withValues(alpha: dot != null ? 0.95 : 0.16), borderRadius: BorderRadius.circular(99)),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(icon, size: dot != null ? 10 : 14, color: dot ?? Colors.white),
          const SizedBox(width: 6),
          Text(text, style: TextStyle(color: dot != null ? Colors.black87 : Colors.white, fontSize: 12, fontWeight: FontWeight.w600)),
        ]),
      );
}

class _TimingCard extends StatelessWidget {
  const _TimingCard({required this.timing});
  final Timing timing;

  @override
  Widget build(BuildContext context) {
    Widget row(String day, String time) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 6),
          child: Row(children: [
            Expanded(child: Text(day, style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant))),
            Text(time, style: const TextStyle(fontWeight: FontWeight.w700)),
          ]),
        );
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Column(children: [
          row(timing.fridayIn != null ? 'Other days' : 'School days', '${timing.timeIn} - ${timing.timeOut}'),
          if (timing.fridayIn != null) row('Friday', '${timing.fridayIn} - ${timing.fridayOut}'),
        ]),
      ),
    );
  }
}

// ── attendance ───────────────────────────────────────────────────────

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key, required this.student});
  final StudentCard student;

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);

  bool get _isCurrent => _month.year == DateTime.now().year && _month.month == DateTime.now().month;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return Scaffold(
      appBar: AppBar(title: Text('Attendance · ${firstName(widget.student.name)}')),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(8, 0, 8, 4),
          child: Row(children: [
            IconButton(onPressed: () => setState(() => _month = DateTime(_month.year, _month.month - 1)), icon: const Icon(Icons.chevron_left_rounded)),
            Expanded(child: Text(monthYear(_month), textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700))),
            IconButton(onPressed: _isCurrent ? null : () => setState(() => _month = DateTime(_month.year, _month.month + 1)), icon: const Icon(Icons.chevron_right_rounded)),
          ]),
        ),
        Expanded(
          child: AsyncView<AttendanceMonth>(
            key: ValueKey(_month),
            load: () async => AttendanceMonth.fromJson(await api.get('/app/students/${widget.student.studentId}/attendance',
                query: {'year': '${_month.year}', 'month': '${_month.month}'})),
            builder: (context, m, _) => PageList(children: [
              TileGrid(columns: 4, children: [
                _CountTile('Present', m.present, AppColors.present),
                _CountTile('Late', m.late, AppColors.late),
                _CountTile('Absent', m.absent, AppColors.absent),
                _CountTile('Leave', m.leave, AppColors.leave),
              ]),
              const SizedBox(height: 12),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(children: [
                    MonthCalendar(month: _month, days: {for (final d in m.days) d.date.day: d.status}),
                    const SizedBox(height: 12),
                    Wrap(spacing: 12, runSpacing: 6, alignment: WrapAlignment.center, children: [
                      for (final e in attendanceStyle.entries)
                        Row(mainAxisSize: MainAxisSize.min, children: [
                          Container(width: 10, height: 10, decoration: BoxDecoration(color: e.value.$1, shape: BoxShape.circle)),
                          const SizedBox(width: 4),
                          Text(e.value.$2, style: Theme.of(context).textTheme.labelSmall),
                        ]),
                    ]),
                  ]),
                ),
              ),
              if (m.percent != null) ...[
                const SizedBox(height: 12),
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.insights_rounded, color: AppColors.present),
                    title: Text('${pct(m.percent)} attendance'),
                    subtitle: const Text('Present and late days out of the days marked'),
                  ),
                ),
              ],
              if (m.days.any((d) => d.status == 'A' || d.status == 'L' || (d.status == 'H' && d.note != null))) ...[
                const SectionTitle('Days off'),
                GroupCard(children: [
                  for (final d in m.days.where((d) => d.status == 'A' || d.status == 'L' || (d.status == 'H' && d.note != null)))
                    ListTile(
                      dense: true,
                      leading: Icon(Icons.circle, size: 12, color: attendanceStyle[d.status]!.$1),
                      title: Text(dayShort(d.date)),
                      subtitle: d.note == null ? null : Text(d.note!),
                      trailing: Text(attendanceStyle[d.status]!.$2, style: TextStyle(color: attendanceStyle[d.status]!.$1, fontWeight: FontWeight.w600)),
                    ),
                ]),
              ],
            ]),
          ),
        ),
      ]),
    );
  }
}

class _CountTile extends StatelessWidget {
  const _CountTile(this.label, this.count, this.color);
  final String label;
  final int count;
  final Color color;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Column(children: [
            Text('$count', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800, color: color)),
            Text(label, style: Theme.of(context).textTheme.labelSmall),
          ]),
        ),
      );
}

/// A month, Monday first, each day coloured by its mark.
class MonthCalendar extends StatelessWidget {
  const MonthCalendar({super.key, required this.month, required this.days});
  final DateTime month;
  final Map<int, String> days;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final first = DateTime(month.year, month.month);
    final count = DateTime(month.year, month.month + 1, 0).day;
    final lead = first.weekday - 1;
    final today = DateTime.now();
    final cells = <Widget>[
      for (final w in const ['M', 'T', 'W', 'T', 'F', 'S', 'S'])
        Center(child: Text(w, style: TextStyle(fontWeight: FontWeight.w700, color: cs.onSurfaceVariant, fontSize: 12))),
      for (var i = 0; i < lead; i++) const SizedBox(),
      for (var day = 1; day <= count; day++)
        Builder(builder: (context) {
          final st = days[day] ?? '';
          final c = attendanceStyle[st]?.$1;
          final isToday = today.year == month.year && today.month == month.month && today.day == day;
          final strong = st == 'P' || st == 'T' || st == 'A' || st == 'L';
          return Center(
            child: Container(
              width: 36,
              height: 36,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: c == null ? null : (strong ? c : c.withValues(alpha: 0.15)),
                shape: BoxShape.circle,
                border: isToday ? Border.all(color: cs.onSurface, width: 2) : null,
              ),
              child: Text('$day', style: TextStyle(fontWeight: FontWeight.w600, color: strong ? Colors.white : (c ?? cs.onSurface))),
            ),
          );
        }),
    ];
    return GridView.count(
      crossAxisCount: 7,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 4,
      childAspectRatio: 1,
      children: cells,
    );
  }
}

// ── fees ─────────────────────────────────────────────────────────────

class FeesScreen extends StatelessWidget {
  const FeesScreen({super.key, required this.student});
  final StudentCard student;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final id = student.studentId;
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text('Fees · ${firstName(student.name)}'),
          bottom: const TabBar(tabs: [Tab(text: 'Dues'), Tab(text: 'Ledger')]),
        ),
        body: TabBarView(children: [
          AsyncView<Fees>(
            load: () async => Fees.fromJson(await api.get('/app/students/$id/fees')),
            builder: (context, f, _) {
              final byMonth = <DateTime, List<FeeDue>>{};
              for (final d in f.dues) {
                byMonth.putIfAbsent(d.month, () => []).add(d);
              }
              return PageList(children: [
                _DueCard(total: f.duesTotal, months: byMonth.length),
                const SizedBox(height: 12),
                Row(children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => PdfScreen.open(context, title: 'Fee voucher', path: '/app/students/$id/voucher.pdf', fileName: 'Fee-voucher-${student.rollNo ?? id}.pdf'),
                      icon: const Icon(Icons.receipt_rounded),
                      label: const Text('Fee voucher'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => PdfScreen.open(context, title: 'Fee ledger', path: '/app/students/$id/ledger.pdf', fileName: 'Ledger-${student.rollNo ?? id}.pdf'),
                      icon: const Icon(Icons.picture_as_pdf_rounded),
                      label: const Text('Ledger PDF'),
                    ),
                  ),
                ]),
                if (byMonth.isNotEmpty) ...[
                  const SectionTitle('Unpaid'),
                  GroupCard(children: [
                    for (final e in byMonth.entries)
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Row(children: [
                            Expanded(child: Text(monthYear(e.key), style: const TextStyle(fontWeight: FontWeight.w700))),
                            Text(rs(e.value.fold<double>(0, (a, b) => a + b.amount)), style: const TextStyle(fontWeight: FontWeight.w800, color: AppColors.money)),
                          ]),
                          for (final d in e.value)
                            Padding(
                              padding: const EdgeInsets.only(top: 4),
                              child: Row(children: [
                                Expanded(child: Text(d.feeType ?? 'Fee', style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant))),
                                Text(rs(d.amount)),
                              ]),
                            ),
                        ]),
                      ),
                  ]),
                ],
                if (f.lastVoucher != null) ...[
                  const SectionTitle('Last payment'),
                  Card(
                    child: ListTile(
                      leading: const CircleAvatar(backgroundColor: Color(0x2216A34A), child: Icon(Icons.check_rounded, color: AppColors.present)),
                      title: Text(rs(f.lastVoucher!.amount), style: const TextStyle(fontWeight: FontWeight.w800)),
                      subtitle: Text('${d(f.lastVoucher!.date)}  ·  ${f.lastVoucher!.paymentMethod ?? ''}\nVoucher ${f.lastVoucher!.voucherNo ?? ''}'),
                      isThreeLine: true,
                    ),
                  ),
                ],
              ]);
            },
          ),
          AsyncView<List<LedgerRow>>(
            load: () async => ((await api.get('/app/students/$id/ledger')) as List).map((e) => LedgerRow.fromJson(e)).toList().reversed.toList(),
            builder: (context, rows, _) => rows.isEmpty
                ? const PageList(children: [EmptyView(icon: Icons.receipt_long_rounded, title: 'No ledger entries yet')])
                : ListView.separated(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    itemCount: rows.length,
                    separatorBuilder: (_, _) => const Divider(indent: 16, endIndent: 16),
                    itemBuilder: (context, i) {
                      final r = rows[i];
                      final paid = r.credit > 0;
                      return ListTile(
                        leading: CircleAvatar(
                          backgroundColor: (paid ? AppColors.present : AppColors.money).withValues(alpha: 0.12),
                          child: Icon(paid ? Icons.south_west_rounded : Icons.north_east_rounded, color: paid ? AppColors.present : AppColors.money, size: 20),
                        ),
                        title: Text(r.description ?? (paid ? 'Payment' : 'Fee'), maxLines: 2, overflow: TextOverflow.ellipsis),
                        subtitle: Text('${d(r.date)}  ·  Balance ${rs(r.balance)}'),
                        trailing: Text(paid ? '- ${rs(r.credit)}' : rs(r.debit),
                            style: TextStyle(fontWeight: FontWeight.w700, color: paid ? AppColors.present : null)),
                      );
                    },
                  ),
          ),
        ]),
      ),
    );
  }
}

class _DueCard extends StatelessWidget {
  const _DueCard({required this.total, required this.months});
  final double total;
  final int months;

  @override
  Widget build(BuildContext context) {
    final paid = total <= 0;
    final c = paid ? AppColors.present : AppColors.money;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(color: c.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(20), border: Border.all(color: c.withValues(alpha: 0.3))),
      child: Row(children: [
        Icon(paid ? Icons.verified_rounded : Icons.account_balance_wallet_rounded, color: c, size: 40),
        const SizedBox(width: 16),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(paid ? 'All fees paid' : 'Total due', style: TextStyle(color: c, fontWeight: FontWeight.w600)),
            if (!paid) Text(rs(total), style: TextStyle(color: c, fontSize: 28, fontWeight: FontWeight.w900)),
            if (!paid) Text('for $months month${months == 1 ? '' : 's'}', style: Theme.of(context).textTheme.bodySmall),
            if (paid) const Text('Thank you!'),
          ]),
        ),
      ]),
    );
  }
}

// ── results ──────────────────────────────────────────────────────────

class ResultsScreen extends StatelessWidget {
  const ResultsScreen({super.key, required this.student});
  final StudentCard student;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return Scaffold(
      appBar: AppBar(title: Text('Results · ${firstName(student.name)}')),
      body: AsyncView<List<Exam>>(
        load: () async => ((await api.get('/app/students/${student.studentId}/exams')) as List).map((e) => Exam.fromJson(e)).toList(),
        builder: (context, exams, _) => PageList(children: [
          if (exams.isEmpty)
            const EmptyView(icon: Icons.workspace_premium_rounded, title: 'No results yet', subtitle: 'Results appear here once the school announces them.')
          else
            for (final e in exams)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Card(
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => ExamScreen(student: student, exam: e))),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(children: [
                        PercentRing(percent: e.percent ?? 0, size: 64),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text(e.examName, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                            const SizedBox(height: 2),
                            Text('${n0(e.obtainedMarks)} / ${n0(e.totalMarks)} marks', style: Theme.of(context).textTheme.bodyMedium),
                            const SizedBox(height: 6),
                            e.failedSubjects == 0
                                ? const Pill('Passed all subjects', color: AppColors.present, icon: Icons.check_rounded)
                                : Pill('${e.failedSubjects} subject${e.failedSubjects == 1 ? '' : 's'} below pass', color: AppColors.absent),
                          ]),
                        ),
                        const Icon(Icons.chevron_right_rounded),
                      ]),
                    ),
                  ),
                ),
              ),
        ]),
      ),
    );
  }
}

class PercentRing extends StatelessWidget {
  const PercentRing({super.key, required this.percent, this.size = 56});
  final double percent;
  final double size;

  static Color colorFor(double p) => p >= 80 ? AppColors.present : (p >= 60 ? AppColors.leave : (p >= 40 ? AppColors.late : AppColors.absent));

  @override
  Widget build(BuildContext context) {
    final c = colorFor(percent);
    return SizedBox(
      width: size,
      height: size,
      child: Stack(fit: StackFit.expand, children: [
        CircularProgressIndicator(value: (percent / 100).clamp(0, 1), strokeWidth: size / 9, color: c, backgroundColor: c.withValues(alpha: 0.15), strokeCap: StrokeCap.round),
        Center(child: Text('${percent.round()}%', style: TextStyle(fontWeight: FontWeight.w800, fontSize: size * 0.24, color: c))),
      ]),
    );
  }
}

class ExamScreen extends StatelessWidget {
  const ExamScreen({super.key, required this.student, required this.exam});
  final StudentCard student;
  final Exam exam;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      appBar: AppBar(title: Text(exam.examName)),
      body: AsyncView<List<ExamSubject>>(
        load: () async => ((await api.get('/app/students/${student.studentId}/exams/${exam.examId}')) as List).map((e) => ExamSubject.fromJson(e)).toList(),
        builder: (context, subjects, _) => PageList(children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(children: [
                PercentRing(percent: exam.percent ?? 0, size: 76),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(student.name, style: t.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                    Text(student.classLabel, style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                    const SizedBox(height: 6),
                    Text('${n0(exam.obtainedMarks)} out of ${n0(exam.totalMarks)}', style: t.titleSmall),
                  ]),
                ),
              ]),
            ),
          ),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: () => PdfScreen.open(context,
                title: 'Result card', path: '/app/students/${student.studentId}/exams/${exam.examId}/card.pdf', fileName: 'Result-${student.rollNo ?? student.studentId}.pdf'),
            icon: const Icon(Icons.picture_as_pdf_rounded),
            label: const Text('Result card (PDF)'),
          ),
          const SectionTitle('Subjects'),
          GroupCard(children: [
            for (final s in subjects)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Expanded(child: Text(s.subjectName, style: const TextStyle(fontWeight: FontWeight.w600))),
                    if (s.absent)
                      const Pill('Absent', color: AppColors.absent)
                    else
                      Text('${s.obtained == null ? '-' : n0(s.obtained!)} / ${n0(s.totalMarks)}',
                          style: TextStyle(fontWeight: FontWeight.w800, color: s.passed ? null : AppColors.absent)),
                  ]),
                  const SizedBox(height: 8),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(99),
                    child: LinearProgressIndicator(
                      minHeight: 6,
                      value: s.totalMarks == 0 ? 0 : ((s.obtained ?? 0) / s.totalMarks).clamp(0, 1),
                      color: s.passed ? AppColors.present : AppColors.absent,
                      backgroundColor: cs.surfaceContainerHighest,
                    ),
                  ),
                  if (!s.passed && !s.absent)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text('Pass marks ${n0(s.passMarks)}', style: t.labelSmall?.copyWith(color: AppColors.absent)),
                    ),
                ]),
              ),
          ]),
        ]),
      ),
    );
  }
}

// ── diary ────────────────────────────────────────────────────────────

class DiaryScreen extends StatefulWidget {
  const DiaryScreen({super.key, required this.student});
  final StudentCard student;

  @override
  State<DiaryScreen> createState() => _DiaryScreenState();
}

class _DiaryScreenState extends State<DiaryScreen> {
  int _days = 14;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final to = DateTime.now();
    final from = to.subtract(Duration(days: _days));
    return Scaffold(
      appBar: AppBar(title: Text('Diary · ${firstName(widget.student.name)}')),
      body: AsyncView<List<DiaryPage>>(
        key: ValueKey(_days),
        load: () async => ((await api.get('/app/students/${widget.student.studentId}/diary', query: {'from': ymd(from), 'to': ymd(to)})) as List)
            .map((e) => DiaryPage.fromJson(e))
            .toList(),
        builder: (context, pages, _) => PageList(children: [
          if (pages.isEmpty)
            EmptyView(icon: Icons.auto_stories_rounded, title: 'No diary in the last $_days days', subtitle: 'The class diary appears here when the teacher posts it.')
          else
            for (final p in pages)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Card(
                  clipBehavior: Clip.antiAlias,
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 14, 16, 8),
                      child: Row(children: [
                        const Icon(Icons.event_note_rounded, color: AppColors.holiday, size: 20),
                        const SizedBox(width: 8),
                        Text(dayShort(p.date), style: const TextStyle(fontWeight: FontWeight.w700)),
                      ]),
                    ),
                    if ((p.notes ?? '').isNotEmpty) Padding(padding: const EdgeInsets.fromLTRB(16, 0, 16, 12), child: Text(p.notes!)),
                    for (final img in p.imageIds) Padding(padding: const EdgeInsets.only(top: 2), child: AuthImage('/app/diary-images/$img', height: 260)),
                  ]),
                ),
              ),
          if (_days < 120)
            Center(
              child: TextButton.icon(onPressed: () => setState(() => _days += 30), icon: const Icon(Icons.history_rounded), label: const Text('Show older')),
            ),
        ]),
      ),
    );
  }
}
