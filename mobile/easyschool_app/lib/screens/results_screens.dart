import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/format.dart';
import '../core/models.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../ui/widgets.dart';
import 'student_screens.dart';

/// Every finalised result, for every member of staff, read only:
/// exam -> class and section -> the students -> one student's marks and result card.
/// An exam still being marked never shows (the server lists finalised exams only).
class StaffResultsScreen extends StatefulWidget {
  const StaffResultsScreen({super.key});

  @override
  State<StaffResultsScreen> createState() => _StaffResultsScreenState();
}

class _StaffResultsScreenState extends State<StaffResultsScreen> {
  int? _examId;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return Scaffold(
      appBar: AppBar(title: const Text('Results')),
      body: AsyncView<List<Map<String, dynamic>>>(
        scrollable: false,   // a column: the exam chips above the classes' own list
        load: () async => ((await api.get('/app/results/exams')) as List).cast<Map<String, dynamic>>(),
        builder: (context, exams, _) {
          if (exams.isEmpty) {
            return const EmptyView(icon: Icons.workspace_premium_rounded, title: 'No results yet', subtitle: 'Results appear here once the school announces them.');
          }
          final examId = _examId ?? exams.first['examId'] as int;
          final exam = exams.firstWhere((e) => e['examId'] == examId, orElse: () => exams.first);
          return Column(children: [
            SizedBox(
              height: 56,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                children: [
                  for (final e in exams)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(e['examName'] ?? ''),
                        selected: e['examId'] == examId,
                        onSelected: (_) => setState(() => _examId = e['examId'] as int),
                      ),
                    ),
                ],
              ),
            ),
            Expanded(child: _Sections(key: ValueKey(examId), examId: examId, examName: exam['examName'] ?? '')),
          ]);
        },
      ),
    );
  }
}

class _Sections extends StatelessWidget {
  const _Sections({super.key, required this.examId, required this.examName});
  final int examId;
  final String examName;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return AsyncView<List<Map<String, dynamic>>>(
      load: () async => ((await api.get('/app/results/$examId/sections')) as List).cast<Map<String, dynamic>>(),
      builder: (context, sections, _) {
        if (sections.isEmpty) return const EmptyView(icon: Icons.groups_rounded, title: 'No classes', subtitle: 'No marks in this exam.');
        // One group per class, its sections inside.
        final classes = <String, List<Map<String, dynamic>>>{};
        for (final s in sections) {
          classes.putIfAbsent(s['className'] ?? '', () => []).add(s);
        }
        return PageList(children: [
          for (final c in classes.entries) ...[
            SectionTitle(RegExp(r'^\d+$').hasMatch(c.key) ? 'Class ${c.key}' : c.key),
            GroupCard(children: [
              for (final s in c.value)
                MenuTile(
                  icon: Icons.groups_rounded,
                  color: AppColors.leave,
                  title: 'Section ${s['sectionName'] ?? ''}',
                  subtitle: '${s['students']} students',
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => SectionResultScreen(examId: examId, examName: examName, section: s))),
                ),
            ]),
          ],
        ]);
      },
    );
  }
}

class SectionResultScreen extends StatelessWidget {
  const SectionResultScreen({super.key, required this.examId, required this.examName, required this.section});
  final int examId;
  final String examName;
  final Map<String, dynamic> section;

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    final t = Theme.of(context).textTheme;
    final cls = section['className'] ?? '';
    final title = '${RegExp(r'^\d+$').hasMatch(cls) ? 'Class $cls' : cls} - ${section['sectionName'] ?? ''}';
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: AsyncView<List<Map<String, dynamic>>>(
        load: () async => ((await api.get('/app/results/$examId/sections/${section['sectionId']}')) as List).cast<Map<String, dynamic>>(),
        builder: (context, rows, _) {
          if (rows.isEmpty) return const EmptyView(icon: Icons.workspace_premium_rounded, title: 'No results', subtitle: 'Nobody in this section has marks in this exam.');
          final passed = rows.where((r) => r['passed'] == true).length;
          return PageList(children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(children: [
                  PercentRing(percent: rows.isEmpty ? 0 : passed * 100 / rows.length, size: 64),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(examName, style: t.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                      const SizedBox(height: 2),
                      Text('$passed of ${rows.length} passed every subject', style: t.bodyMedium),
                    ]),
                  ),
                ]),
              ),
            ),
            const SizedBox(height: 8),
            GroupCard(children: [
              for (final r in rows)
                ListTile(
                  leading: CircleAvatar(
                    backgroundColor: (r['passed'] == true ? AppColors.present : AppColors.absent).withValues(alpha: 0.12),
                    child: Text(r['position'] == null ? '-' : '${r['position']}',
                        style: TextStyle(fontWeight: FontWeight.w800, color: r['passed'] == true ? AppColors.present : AppColors.absent)),
                  ),
                  title: Text(r['name'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text('Roll ${r['rollNo'] ?? '-'} · ${n0(_d(r['obtained']))} / ${n0(_d(r['total']))}'),
                  trailing: Column(mainAxisAlignment: MainAxisAlignment.center, crossAxisAlignment: CrossAxisAlignment.end, children: [
                    Text(r['percent'] == null ? '-' : '${_d(r['percent']).toStringAsFixed(1)}%', style: const TextStyle(fontWeight: FontWeight.w800)),
                    Text(r['passed'] == true ? 'Pass' : 'Fail',
                        style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: r['passed'] == true ? AppColors.present : AppColors.absent)),
                  ]),
                  onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => ExamScreen(
                        student: StudentCard.fromJson({
                          'studentId': r['studentId'], 'name': r['name'], 'rollNo': r['rollNo'], 'fatherName': r['fatherName'],
                          'classId': section['classId'], 'className': section['className'], 'sectionId': section['sectionId'], 'sectionName': section['sectionName'],
                        }),
                        exam: Exam.fromJson({
                          'examId': examId, 'examName': examName, 'totalMarks': r['total'], 'obtainedMarks': r['obtained'],
                          'percent': r['percent'], 'failedSubjects': r['passed'] == true ? 0 : 1,
                        }),
                      ),
                    ),
                  ),
                ),
            ]),
          ]);
        },
      ),
    );
  }

  static double _d(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
}
