import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/models.dart';
import '../core/push.dart';
import '../core/session.dart';
import 'account_screens.dart';
import 'announcements.dart';
import 'chat_screen.dart';
import 'staff_screens.dart';
import 'student_screens.dart';
import 'update.dart';

/// The signed-in app: a home of its own for parents, students and staff,
/// then the school's notices and the account.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _tab = 0;

  @override
  void initState() {
    super.initState();
    Push.instance.onOpenAnnouncement = _openAnnouncement;
    final pending = Push.instance.takePendingOpen();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (pending != null) {
        _openAnnouncement(pending);
      } else {
        offerUpdate(context);
      }
    });
  }

  @override
  void dispose() {
    if (Push.instance.onOpenAnnouncement == _openAnnouncement) Push.instance.onOpenAnnouncement = null;
    super.dispose();
  }

  List<_Tab> _tabs(Me me) => [
        if (me.isStaff)
          const _Tab(Icons.space_dashboard_outlined, Icons.space_dashboard_rounded, 'Home', StaffHome())
        else if (me.isStudent && me.students.isNotEmpty)
          _Tab(Icons.home_outlined, Icons.home_rounded, 'Home', StudentHome(student: me.students.first, asTab: true))
        else
          const _Tab(Icons.home_outlined, Icons.home_rounded, 'Home', FamilyTab()),
        // The school's WhatsApp menu inside the app (free - nothing goes through Meta).
        const _Tab(Icons.chat_outlined, Icons.chat_rounded, 'Chat', ChatTab()),
        if (me.isStaff && me.can(Feature.classView))
          const _Tab(Icons.groups_outlined, Icons.groups_rounded, 'Classes', ClassesTab()),
        const _Tab(Icons.campaign_outlined, Icons.campaign_rounded, 'Notices', AnnouncementsTab()),
        const _Tab(Icons.person_outline_rounded, Icons.person_rounded, 'Account', AccountTab()),
      ];

  void _openAnnouncement(int id) {
    final me = mounted ? context.read<Session>().me : null;
    if (me == null) return;
    final tabs = _tabs(me);
    setState(() => _tab = tabs.indexWhere((t) => t.label == 'Notices'));
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => AnnouncementLoader(id: id)));
  }

  @override
  Widget build(BuildContext context) {
    final me = context.watch<Session>().me;
    // Signing out: the old home fades out while the sign-in screen fades in.
    if (me == null) return const Scaffold();
    final tabs = _tabs(me);
    final index = _tab.clamp(0, tabs.length - 1);
    return Scaffold(
      body: IndexedStack(index: index, children: [for (final t in tabs) t.page]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (i) => setState(() => _tab = i),
        destinations: [
          for (final t in tabs) NavigationDestination(icon: Icon(t.icon), selectedIcon: Icon(t.selected), label: t.label),
        ],
      ),
    );
  }
}

class _Tab {
  const _Tab(this.icon, this.selected, this.label, this.page);
  final IconData icon, selected;
  final String label;
  final Widget page;
}
