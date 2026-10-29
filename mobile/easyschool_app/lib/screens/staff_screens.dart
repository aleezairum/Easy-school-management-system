import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api.dart';
import '../core/format.dart';
import '../core/models.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../ui/widgets.dart';
import 'announcements.dart';
import 'pdf_screen.dart';
import 'results_screens.dart';
import 'student_screens.dart';

Color staffStatusColor(String s) {
  final v = s.toLowerCase();
  if (v.contains('absent')) return AppColors.absent;
  if (v.contains('late')) return AppColors.late;
  if (v.contains('leave')) return AppColors.leave;
  if (v.contains('holiday')) return AppColors.holiday;
  if (v.contains('present')) return AppColors.present;
  return AppColors.off;
}

// ── staff home ───────────────────────────────────────────────────────

class StaffHome extends StatelessWidget {
  const StaffHome({super.key});

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final me = s.me;
    if (me == null) return const SizedBox.shrink(); // signing out
    final api = s.api;
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    void go(Widget page) => Navigator.push(context, MaterialPageRoute(builder: (_) => page));
    return Scaffold(
      body: SafeArea(
        child: AsyncView<StaffSummary>(
          load: () async => StaffSummary.fromJson(await api.get('/app/staff/summary')),
          builder: (context, sum, reload) {
            final p = sum.lastPayslip;
            return PageList(padding: const EdgeInsets.fromLTRB(16, 16, 16, 24), children: [
              Row(children: [
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(greeting(), style: t.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
                    Text(sum.staff.name, style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
                    Text([sum.staff.designation, sum.staff.campusName].whereType<String>().join(' · '), style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                  ]),
                ),
                InitialsAvatar(sum.staff.name, size: 52),
              ]),
              const SizedBox(height: 16),
              _TodayCard(sum: sum),
              const SizedBox(height: 12),
              const LatestNotice(),
              SectionTitle('${monthName(DateTime.now().month)} so far', top: 12),
              TileGrid(children: [
                StatTile(icon: Icons.check_circle_rounded, color: AppColors.present, label: 'Days present', value: '${sum.monthPresent}', onTap: () => go(const StaffAttendanceScreen())),
                StatTile(icon: Icons.cancel_rounded, color: AppColors.absent, label: 'Days absent', value: '${sum.monthAbsent}', onTap: () => go(const StaffAttendanceScreen())),
                StatTile(icon: Icons.beach_access_rounded, color: AppColors.leave, label: 'Leave days', value: '${sum.monthLeave}', onTap: () => go(const StaffAttendanceScreen())),
                StatTile(icon: Icons.timer_rounded, color: AppColors.late, label: 'Late minutes', value: '${sum.monthLateMinutes}', onTap: () => go(const StaffAttendanceScreen())),
              ]),
              if (p != null) ...[
                const SectionTitle('Latest salary'),
                Card(
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    onTap: () => go(const PayslipsScreen()),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(children: [
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text('${monthName(p.month)} ${p.year}', style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                            Text(rs(p.takeHome), style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w900)),
                            Text('Gross ${rs(p.gross)}  ·  Deductions ${rs(p.deduction)}', style: t.bodySmall),
                          ]),
                        ),
                        Pill(p.isFinal ? 'Final' : 'Draft', color: p.isFinal ? AppColors.present : AppColors.late),
                      ]),
                    ),
                  ),
                ),
              ],
              const SectionTitle('Quick links'),
              GroupCard(children: [
                MenuTile(icon: Icons.calendar_month_rounded, color: AppColors.present, title: 'My attendance', subtitle: 'Time in, time out, late minutes', onTap: () => go(const StaffAttendanceScreen())),
                MenuTile(icon: Icons.payments_rounded, color: AppColors.money, title: 'Salary slips', onTap: () => go(const PayslipsScreen())),
                // Every member of staff: every announced result, read only.
                MenuTile(icon: Icons.workspace_premium_rounded, color: AppColors.present, title: 'Results', subtitle: 'Every class, announced exams', onTap: () => go(const StaffResultsScreen())),
                if (me.can(Feature.classView))
                  MenuTile(icon: Icons.groups_rounded, color: AppColors.leave, title: 'My classes', subtitle: '${sum.classCount} class${sum.classCount == 1 ? '' : 'es'}', onTap: () => go(const ClassesTab(standalone: true))),
                if (me.students.isNotEmpty)
                  MenuTile(icon: Icons.family_restroom_rounded, color: const Color(0xFFDB2777), title: 'My children', subtitle: me.students.map((x) => firstName(x.name)).join(', '), onTap: () => go(const MyChildrenScreen())),
                if (me.can(Feature.adminDashboard))
                  MenuTile(icon: Icons.insights_rounded, color: AppColors.brand, title: 'School dashboard', subtitle: 'Students, fee collection, attendance', onTap: () => go(const AdminDashboardScreen())),
                if (me.can(Feature.adminStudents))
                  MenuTile(icon: Icons.person_search_rounded, color: AppColors.holiday, title: 'Find a student', subtitle: 'Profile, fees, attendance, results', onTap: () => go(const StudentSearchScreen())),
                if (me.can(Feature.announceSend))
                  MenuTile(icon: Icons.campaign_rounded, color: AppColors.absent, title: 'Send a notice', subtitle: 'Parents get it on their phones', onTap: () => go(const PostAnnouncementScreen())),
              ]),
            ]);
          },
        ),
      ),
    );
  }
}

class _TodayCard extends StatelessWidget {
  const _TodayCard({required this.sum});
  final StaffSummary sum;

  @override
  Widget build(BuildContext context) {
    final status = sum.todayStatus;
    final c = status == null ? AppColors.off : staffStatusColor(status);
    Widget time(String label, String? v) => Expanded(
          child: Column(children: [
            Text(v ?? '--:--', style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w800)),
            Text(label, style: TextStyle(color: Colors.white.withValues(alpha: 0.8), fontSize: 12)),
          ]),
        );
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        gradient: const LinearGradient(colors: [AppColors.brand, AppColors.brandDark], begin: Alignment.topLeft, end: Alignment.bottomRight),
      ),
      child: Column(children: [
        Row(children: [
          const Icon(Icons.today_rounded, color: Colors.white),
          const SizedBox(width: 8),
          Expanded(child: Text('Today · ${dayShort(DateTime.now())}', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700))),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(99)),
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Icon(Icons.circle, size: 10, color: c),
              const SizedBox(width: 6),
              Text(status ?? 'Not marked', style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 12, color: Colors.black87)),
            ]),
          ),
        ]),
        const SizedBox(height: 16),
        Row(children: [
          time('Time in', sum.todayIn),
          Container(width: 1, height: 36, color: Colors.white24),
          time('Time out', sum.todayOut),
        ]),
      ]),
    );
  }
}

// ── my attendance ────────────────────────────────────────────────────

class StaffAttendanceScreen extends StatefulWidget {
  const StaffAttendanceScreen({super.key});

  @override
  State<StaffAttendanceScreen> createState() => _StaffAttendanceScreenState();
}

class _StaffAttendanceScreenState extends State<StaffAttendanceScreen> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);
  bool get _isCurrent => _month.year == DateTime.now().year && _month.month == DateTime.now().month;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return Scaffold(
      appBar: AppBar(
        title: const Text('My attendance'),
        actions: [
          IconButton(
            tooltip: 'Attendance card (PDF)',
            icon: const Icon(Icons.picture_as_pdf_rounded),
            onPressed: () => PdfScreen.open(context,
                title: 'Attendance card', path: '/app/staff/attendance-card/${_month.year}/${_month.month}.pdf', fileName: 'Attendance-${_month.year}-${_month.month}.pdf'),
          ),
        ],
      ),
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
          child: AsyncView<List<StaffDay>>(
            key: ValueKey(_month),
            load: () async => ((await api.get('/app/staff/attendance', query: {'year': '${_month.year}', 'month': '${_month.month}'})) as List)
                .map((e) => StaffDay.fromJson(e))
                .toList()
                .reversed
                .toList(),
            builder: (context, days, _) => days.isEmpty
                ? const PageList(children: [EmptyView(icon: Icons.event_busy_rounded, title: 'Nothing recorded this month')])
                : PageList(children: [
                    GroupCard(children: [
                      for (final d in days)
                        ListTile(
                          leading: SizedBox(
                            width: 44,
                            child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                              Text('${d.date.day}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                              Text(['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'][d.date.weekday - 1], style: Theme.of(context).textTheme.labelSmall),
                            ]),
                          ),
                          title: Text(d.timeIn == null ? '—' : '${d.timeIn}  →  ${d.timeOut ?? '…'}'),
                          subtitle: d.lateMinutes > 0 || d.earlyMinutes > 0
                              ? Text([if (d.lateMinutes > 0) '${d.lateMinutes} min late', if (d.earlyMinutes > 0) '${d.earlyMinutes} min early out'].join(' · '),
                                  style: const TextStyle(color: AppColors.late))
                              : null,
                          trailing: Pill(d.status.isEmpty ? 'Not marked' : d.status, color: staffStatusColor(d.status)),
                        ),
                    ]),
                  ]),
          ),
        ),
      ]),
    );
  }
}

// ── salary slips ─────────────────────────────────────────────────────

class PayslipsScreen extends StatelessWidget {
  const PayslipsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final t = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Salary slips')),
      body: AsyncView<List<Payslip>>(
        load: () async => ((await api.get('/app/staff/payslips')) as List).map((e) => Payslip.fromJson(e)).toList(),
        builder: (context, slips, _) => PageList(children: [
          if (slips.isEmpty)
            const EmptyView(icon: Icons.payments_rounded, title: 'No salary slips yet')
          else
            for (final p in slips)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Card(
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    onTap: () => PdfScreen.open(context,
                        title: 'Salary slip · ${monthName(p.month)} ${p.year}', path: '/app/staff/payslips/${p.year}/${p.month}.pdf', fileName: 'Salary-slip-${p.year}-${p.month}.pdf'),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Row(children: [
                          Expanded(child: Text('${monthName(p.month)} ${p.year}', style: t.titleMedium?.copyWith(fontWeight: FontWeight.w700))),
                          Pill(p.isFinal ? (p.paidDate != null ? 'Paid ${d(p.paidDate!)}' : 'Final') : 'Draft', color: p.isFinal ? AppColors.present : AppColors.late),
                        ]),
                        const SizedBox(height: 10),
                        Row(children: [
                          _Amount('Gross', p.gross),
                          _Amount('Deductions', p.deduction, color: AppColors.absent),
                          _Amount('Take home', p.takeHome, color: AppColors.brand, bold: true),
                        ]),
                        const SizedBox(height: 10),
                        Row(children: [
                          Icon(Icons.picture_as_pdf_rounded, size: 16, color: Theme.of(context).colorScheme.primary),
                          const SizedBox(width: 6),
                          Text('View slip', style: TextStyle(color: Theme.of(context).colorScheme.primary, fontWeight: FontWeight.w600)),
                        ]),
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

class _Amount extends StatelessWidget {
  const _Amount(this.label, this.value, {this.color, this.bold = false});
  final String label;
  final double value;
  final Color? color;
  final bool bold;

  @override
  Widget build(BuildContext context) => Expanded(
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(label, style: Theme.of(context).textTheme.labelSmall),
          FittedBox(child: Text(rs(value), style: TextStyle(fontWeight: bold ? FontWeight.w900 : FontWeight.w600, color: color, fontSize: bold ? 17 : 14))),
        ]),
      );
}

// ── my children (staff) ──────────────────────────────────────────────

class MyChildrenScreen extends StatelessWidget {
  const MyChildrenScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final me = context.read<Session>().me!;
    return Scaffold(
      appBar: AppBar(title: const Text('My children')),
      body: PageList(children: [
        for (final st in me.students) Padding(padding: const EdgeInsets.only(bottom: 12), child: ChildCard(student: st)),
      ]),
    );
  }
}

// ── classes ──────────────────────────────────────────────────────────

class ClassesTab extends StatelessWidget {
  const ClassesTab({super.key, this.standalone = false});
  final bool standalone;

  @override
  Widget build(BuildContext context) {
    final s = context.read<Session>();
    final canMark = s.me!.can(Feature.classAttendance);
    return Scaffold(
      appBar: AppBar(title: const Text('My classes'), automaticallyImplyLeading: standalone),
      body: AsyncView<List<TeacherClass>>(
        load: () async => ((await s.api.get('/app/staff/classes')) as List).map((e) => TeacherClass.fromJson(e)).toList(),
        builder: (context, classes, _) => PageList(children: [
          if (classes.isEmpty)
            const EmptyView(icon: Icons.groups_rounded, title: 'No classes allocated', subtitle: 'Classes come from Subject Allocation in the school system.')
          else
            for (final c in classes)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Card(
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => ClassAttendanceScreen(cls: c, canMark: canMark))),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(children: [
                        Container(
                          width: 52,
                          height: 52,
                          alignment: Alignment.center,
                          decoration: BoxDecoration(color: AppColors.leave.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(14)),
                          child: Text(c.sectionName ?? '', style: const TextStyle(color: AppColors.leave, fontWeight: FontWeight.w800, fontSize: 16)),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text(c.label, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                            Text(c.subjects.join(', '), maxLines: 2, overflow: TextOverflow.ellipsis, style: Theme.of(context).textTheme.bodySmall),
                            const SizedBox(height: 6),
                            Pill('${c.students} students', color: AppColors.brand, icon: Icons.people_alt_rounded),
                          ]),
                        ),
                        Column(children: [
                          Icon(canMark ? Icons.fact_check_rounded : Icons.visibility_rounded, color: Theme.of(context).colorScheme.primary),
                          Text(canMark ? 'Attendance' : 'View', style: Theme.of(context).textTheme.labelSmall),
                        ]),
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

class ClassAttendanceScreen extends StatefulWidget {
  const ClassAttendanceScreen({super.key, required this.cls, required this.canMark});
  final TeacherClass cls;
  final bool canMark;

  @override
  State<ClassAttendanceScreen> createState() => _ClassAttendanceScreenState();
}

class _ClassAttendanceScreenState extends State<ClassAttendanceScreen> {
  DateTime _date = DateTime.now();
  ClassAttendance? _data;
  Object? _error;
  bool _loading = true, _saving = false, _dirty = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final j = await context.read<Session>().api.get('/app/classes/${widget.cls.sectionId}/attendance', query: {'date': ymd(_date)});
      setState(() {
        _data = ClassAttendance.fromJson(j);
        _dirty = false;
      });
    } catch (e) {
      setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _save() async {
    final rows = _data!.rows;
    final unmarked = rows.where((r) => r.status.isEmpty).length;
    if (unmarked > 0) {
      showMessage(context, '$unmarked student${unmarked == 1 ? ' is' : 's are'} not marked yet.', error: true);
      return;
    }
    setState(() => _saving = true);
    try {
      final res = await context.read<Session>().api.post('/app/classes/${widget.cls.sectionId}/attendance', {
        'date': ymd(_date),
        'rows': [for (final r in rows) {'studentId': r.studentId, 'status': r.status}],
      });
      if (!mounted) return;
      showMessage(context, 'Attendance saved for ${res['saved']} students.');
      await _load();
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _set(ClassAttendanceRow r, String v) => setState(() {
        r.status = v;
        _dirty = true;
      });

  @override
  Widget build(BuildContext context) {
    final data = _data;
    final counts = {for (final k in ['P', 'T', 'A', 'L']) k: data?.rows.where((r) => r.status == k).length ?? 0};
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.cls.label),
        actions: [
          if (widget.canMark && data != null)
            TextButton(
              onPressed: () => setState(() {
                for (final r in data.rows) {
                  if (r.status.isEmpty) r.status = 'P';
                }
                _dirty = true;
              }),
              child: const Text('Rest present'),
            ),
        ],
      ),
      bottomNavigationBar: widget.canMark && data != null && data.rows.isNotEmpty
          ? SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton.icon(
                  onPressed: _saving || (!_dirty && data.marked) ? null : _save,
                  icon: _saving ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white)) : const Icon(Icons.save_rounded),
                  label: Text(data.marked && !_dirty ? 'Saved' : (data.marked ? 'Update attendance' : 'Save attendance')),
                ),
              ),
            )
          : null,
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(children: [
            OutlinedButton.icon(
              icon: const Icon(Icons.event_rounded, size: 18),
              label: Text(dayShort(_date)),
              onPressed: () async {
                final now = DateTime.now();
                final v = await showDatePicker(context: context, firstDate: now.subtract(const Duration(days: 60)), lastDate: now, initialDate: _date);
                if (v != null) {
                  setState(() => _date = v);
                  _load();
                }
              },
            ),
            const Spacer(),
            if (data != null) Pill(data.marked ? 'Marked' : 'Not marked yet', color: data.marked ? AppColors.present : AppColors.late),
          ]),
        ),
        if (data != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: Row(children: [
              for (final e in counts.entries)
                Expanded(
                  child: Container(
                    margin: const EdgeInsets.symmetric(horizontal: 3),
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    decoration: BoxDecoration(color: attendanceStyle[e.key]!.$1.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(10)),
                    child: Column(children: [
                      Text('${e.value}', style: TextStyle(fontWeight: FontWeight.w800, color: attendanceStyle[e.key]!.$1)),
                      Text(attendanceStyle[e.key]!.$2, style: Theme.of(context).textTheme.labelSmall),
                    ]),
                  ),
                ),
            ]),
          ),
        Expanded(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : _error != null
                  ? ErrorView(message: _error.toString(), onRetry: _load)
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: ListView.separated(
                        padding: const EdgeInsets.only(bottom: 16),
                        itemCount: data!.rows.length,
                        separatorBuilder: (_, _) => const Divider(indent: 16, endIndent: 16),
                        itemBuilder: (context, i) {
                          final r = data.rows[i];
                          return Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            child: Row(children: [
                              SizedBox(width: 40, child: Text(r.rollNo ?? '', style: Theme.of(context).textTheme.labelMedium)),
                              Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text(r.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                  if (r.fatherName != null) Text(r.fatherName!, style: Theme.of(context).textTheme.bodySmall),
                                ]),
                              ),
                              if (widget.canMark)
                                _MarkPicker(value: r.status, onChanged: (v) => _set(r, v))
                              else
                                Pill(attendanceStyle[r.status]?.$2 ?? 'Not marked', color: attendanceStyle[r.status]?.$1 ?? AppColors.off),
                            ]),
                          );
                        },
                      ),
                    ),
        ),
      ]),
    );
  }
}

class _MarkPicker extends StatelessWidget {
  const _MarkPicker({required this.value, required this.onChanged});
  final String value;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) => Row(mainAxisSize: MainAxisSize.min, children: [
        for (final k in const ['P', 'T', 'A', 'L'])
          Padding(
            padding: const EdgeInsets.only(left: 4),
            child: InkWell(
              borderRadius: BorderRadius.circular(10),
              onTap: () => onChanged(k),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 150),
                width: 36,
                height: 36,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: value == k ? attendanceStyle[k]!.$1 : Colors.transparent,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: value == k ? attendanceStyle[k]!.$1 : Theme.of(context).colorScheme.outlineVariant),
                ),
                child: Text(k, style: TextStyle(fontWeight: FontWeight.w800, color: value == k ? Colors.white : attendanceStyle[k]!.$1)),
              ),
            ),
          ),
      ]);
}

// ── admin ────────────────────────────────────────────────────────────

class AdminDashboardScreen extends StatelessWidget {
  const AdminDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final t = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(title: const Text('School dashboard')),
      body: AsyncView<Map<String, dynamic>>(
        load: () async => (await api.get('/app/admin/dashboard')) as Map<String, dynamic>,
        builder: (context, j, _) {
          final tot = (j['totals'] as Map?) ?? {};
          final fee = (j['fees'] as Map?) ?? {};
          final att = (j['attendance'] as Map?) ?? {};
          num v(Map m, String k) => (m[k] as num?) ?? 0;
          final vouchers = (j['recentVouchers'] as List?) ?? [];
          final admissions = (j['recentStudents'] as List?) ?? [];
          return PageList(children: [
            const SectionTitle('Today', top: 4),
            TileGrid(children: [
              StatTile(icon: Icons.payments_rounded, color: AppColors.present, label: 'Collected today', value: rs(v(fee, 'todayCollection')), sub: '${v(fee, 'todayVoucherCount')} vouchers'),
              StatTile(icon: Icons.how_to_reg_rounded, color: AppColors.leave, label: 'Student attendance', value: pct(v(att, 'todayPercent'))),
            ]),
            const SectionTitle('This month'),
            TileGrid(children: [
              StatTile(icon: Icons.account_balance_rounded, color: AppColors.brand, label: 'Fee collected', value: rs(v(fee, 'monthCollection')), sub: '${v(fee, 'monthVoucherCount')} vouchers'),
              StatTile(icon: Icons.pending_actions_rounded, color: AppColors.money, label: 'Fee pending', value: rs(v(fee, 'pendingFeeTotal')), sub: '${v(fee, 'pendingFeeStudents')} students'),
            ]),
            const SectionTitle('School'),
            TileGrid(columns: 3, children: [
              StatTile(icon: Icons.school_rounded, color: AppColors.leave, label: 'Students', value: n0(v(tot, 'activeStudents'))),
              StatTile(icon: Icons.badge_rounded, color: AppColors.holiday, label: 'Staff', value: n0(v(tot, 'employees'))),
              StatTile(icon: Icons.person_add_alt_1_rounded, color: AppColors.present, label: 'Admissions', value: n0(v(tot, 'newAdmissions'))),
              StatTile(icon: Icons.class_rounded, color: AppColors.brand, label: 'Classes', value: n0(v(tot, 'classes'))),
              StatTile(icon: Icons.view_module_rounded, color: AppColors.late, label: 'Sections', value: n0(v(tot, 'sections'))),
              StatTile(icon: Icons.apartment_rounded, color: AppColors.off, label: 'Campuses', value: n0(v(tot, 'campuses'))),
            ]),
            if (vouchers.isNotEmpty) ...[
              const SectionTitle('Latest payments'),
              GroupCard(children: [
                for (final x in vouchers.take(6))
                  ListTile(
                    dense: true,
                    leading: const Icon(Icons.receipt_rounded, color: AppColors.present),
                    title: Text(rs((x['amount'] as num?) ?? 0), style: const TextStyle(fontWeight: FontWeight.w700)),
                    subtitle: Text('${x['voucherNumber'] ?? ''} · ${x['paymentMethod'] ?? ''}'),
                    trailing: Text(x['paymentDate'] == null ? '' : d(DateTime.parse(x['paymentDate'])), style: t.bodySmall),
                  ),
              ]),
            ],
            if (admissions.isNotEmpty) ...[
              const SectionTitle('Latest admissions'),
              GroupCard(children: [
                for (final x in admissions.take(6))
                  ListTile(
                    dense: true,
                    leading: InitialsAvatar('${x['name'] ?? ''}', size: 36),
                    title: Text('${x['name'] ?? ''}', style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text('Class ${x['className'] ?? ''} ${x['sectionName'] ?? ''} · Roll ${x['rollNo'] ?? ''}'),
                    trailing: Text(x['admissionDate'] == null ? '' : d(DateTime.parse(x['admissionDate'])), style: t.bodySmall),
                  ),
              ]),
            ],
          ]);
        },
      ),
    );
  }
}

class StudentSearchScreen extends StatefulWidget {
  const StudentSearchScreen({super.key});

  @override
  State<StudentSearchScreen> createState() => _StudentSearchScreenState();
}

class _StudentSearchScreenState extends State<StudentSearchScreen> {
  final _q = TextEditingController();
  Timer? _debounce;
  List<StudentCard>? _results;
  bool _busy = false;
  String? _error;

  void _changed(String v) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () => _search(v));
  }

  Future<void> _search(String v) async {
    if (v.trim().length < 2) return setState(() => _results = null);
    setState(() => _busy = true);
    try {
      final list = ((await context.read<Session>().api.get('/app/admin/students', query: {'q': v.trim()})) as List).map((e) => StudentCard.fromJson(e)).toList();
      if (mounted && _q.text == v) {
        setState(() {
          _results = list;
          _error = null;
        });
      }
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Find a student')),
        body: Column(children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: TextField(
              controller: _q,
              autofocus: true,
              onChanged: _changed,
              textInputAction: TextInputAction.search,
              onSubmitted: _search,
              decoration: InputDecoration(
                hintText: 'Name, father\'s name or roll number',
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: _busy ? const Padding(padding: EdgeInsets.all(14), child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))) : null,
              ),
            ),
          ),
          Expanded(
            child: _error != null
                ? ErrorView(message: _error!)
                : _results == null
                    ? const EmptyView(icon: Icons.person_search_rounded, title: 'Search the school', subtitle: 'Type at least 2 letters.')
                    : _results!.isEmpty
                        ? const EmptyView(icon: Icons.search_off_rounded, title: 'No student found')
                        : ListView.separated(
                            itemCount: _results!.length,
                            separatorBuilder: (_, _) => const Divider(indent: 72),
                            itemBuilder: (context, i) {
                              final s = _results![i];
                              return ListTile(
                                leading: InitialsAvatar(s.name, size: 42),
                                title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                subtitle: Text('${s.classLabel} · Roll ${s.rollNo ?? '-'}${s.fatherName != null ? '\nS/O, D/O ${s.fatherName}' : ''}'),
                                isThreeLine: s.fatherName != null,
                                trailing: const Icon(Icons.chevron_right_rounded),
                                onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => StudentHome(student: s))),
                              );
                            },
                          ),
          ),
        ]),
      );
}
