import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/api.dart';
import '../core/push.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../main.dart';
import '../ui/widgets.dart';
import 'update.dart';

const _modes = {
  'parent': (Icons.family_restroom_rounded, 'Parent', 'Sign in with any of your children\'s logins to see all your children.'),
  'student': (Icons.face_rounded, 'Student', 'See your own attendance, results, diary and fees.'),
  'staff': (Icons.badge_rounded, 'Staff', 'Sign in with your employee login.'),
};

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _id = TextEditingController();
  final _pw = TextEditingController();
  String _mode = 'parent';
  bool _hide = true, _busy = false;
  String? _error;

  Future<void> _signIn() async {
    if (_id.text.trim().isEmpty || _pw.text.isEmpty) {
      setState(() => _error = 'Enter your Login ID and password.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await context.read<Session>().signIn(_mode, _id.text, _pw.text);
      // Only now, with a login that worked, may the phone offer to save it.
      TextInput.finishAutofillContext();
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final m = _modes[_mode]!;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                const Center(child: BrandMark(size: 64)),
                const SizedBox(height: 16),
                Text(s.info?.name ?? 'Sign in', textAlign: TextAlign.center, style: t.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                if ((s.info?.address ?? '').isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(s.info!.address!, textAlign: TextAlign.center, style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                  ),
                if (s.signedOutReason != null) ...[
                  const SizedBox(height: 16),
                  Card(
                    color: cs.secondaryContainer,
                    child: Padding(padding: const EdgeInsets.all(12), child: Text(s.signedOutReason!, textAlign: TextAlign.center)),
                  ),
                ],
                const SizedBox(height: 28),
                SegmentedButton<String>(
                  segments: [
                    for (final e in _modes.entries) ButtonSegment(value: e.key, icon: Icon(e.value.$1), label: Text(e.value.$2)),
                  ],
                  selected: {_mode},
                  showSelectedIcon: false,
                  onSelectionChanged: (v) => setState(() {
                    _mode = v.first;
                    _error = null;
                  }),
                ),
                const SizedBox(height: 10),
                Text(m.$3, textAlign: TextAlign.center, style: t.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 20),
                // The phone's password manager (Google, Samsung) fills these and offers to save
                // them. Leaving the screen any other way (Change school) must not offer to save.
                AutofillGroup(
                  onDisposeAction: AutofillContextAction.cancel,
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    TextField(
                      controller: _id,
                      textInputAction: TextInputAction.next,
                      autocorrect: false,
                      autofillHints: const [AutofillHints.username],
                      decoration: InputDecoration(
                        labelText: 'Login ID',
                        hintText: _mode == 'staff' ? 'Your employee code' : 'Your child\'s roll number',
                        prefixIcon: const Icon(Icons.person_outline_rounded),
                      ),
                    ),
                    const SizedBox(height: 14),
                    TextField(
                      controller: _pw,
                      obscureText: _hide,
                      textInputAction: TextInputAction.done,
                      autofillHints: const [AutofillHints.password],
                      onSubmitted: (_) => _signIn(),
                      decoration: InputDecoration(
                        labelText: 'Password',
                        hintText: 'First time: the PIN on your slip',
                        prefixIcon: const Icon(Icons.lock_outline_rounded),
                        suffixIcon: IconButton(
                          icon: Icon(_hide ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                          onPressed: () => setState(() => _hide = !_hide),
                        ),
                      ),
                    ),
                  ]),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 12),
                  Text(_error!, style: TextStyle(color: cs.error)),
                ],
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: _busy ? null : _signIn,
                  child: _busy ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white)) : const Text('Sign in'),
                ),
                const SizedBox(height: 16),
                Text('Forgot your password? The school office can reset it and print a new slip.',
                    textAlign: TextAlign.center, style: t.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 24),
                TextButton.icon(
                  onPressed: () => context.read<Session>().forgetSchool(),
                  icon: const Icon(Icons.swap_horiz_rounded),
                  label: const Text('Change school'),
                ),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key, this.firstTime = false});
  final bool firstTime;

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _old = TextEditingController(), _new = TextEditingController(), _again = TextEditingController();
  late final _login = TextEditingController(text: context.read<Session>().me?.loginId ?? '');
  bool _busy = false, _hide = true;
  String? _error;

  Future<void> _save() async {
    if (_new.text.length < 6) return setState(() => _error = 'Use at least 6 characters.');
    if (_new.text != _again.text) return setState(() => _error = 'The two new passwords are not the same.');
    if (_new.text == _old.text) return setState(() => _error = 'Choose a password different from the PIN.');
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await context.read<Session>().changePassword(_old.text, _new.text);
      // The saved password (often the PIN, saved at first sign-in) is updated to the new one.
      TextInput.finishAutofillContext();
      if (!mounted) return;
      if (!widget.firstTime) {
        Navigator.pop(context);
        showMessage(context, 'Password changed.');
      }
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = context.read<Session>();
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    InputDecoration dec(String label, IconData icon) => InputDecoration(
          labelText: label,
          prefixIcon: Icon(icon),
          suffixIcon: IconButton(icon: Icon(_hide ? Icons.visibility_outlined : Icons.visibility_off_outlined), onPressed: () => setState(() => _hide = !_hide)),
        );
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.firstTime ? 'Set your password' : 'Change password'),
        automaticallyImplyLeading: !widget.firstTime,
        actions: [if (widget.firstTime) TextButton(onPressed: () => s.signOut(), child: const Text('Sign out'))],
      ),
      body: SafeArea(
        child: ListView(padding: const EdgeInsets.all(24), children: [
          if (widget.firstTime) ...[
            Text('Welcome, ${s.me?.name ?? ''}!', style: t.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 8),
            Text('Please choose your own password. You will use it from now on instead of the PIN on the slip.',
                style: t.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
            const SizedBox(height: 24),
          ],
          // The Login ID is part of the form so the password manager knows whose password changed.
          AutofillGroup(
            onDisposeAction: AutofillContextAction.cancel,
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              TextField(
                controller: _login,
                readOnly: true,
                autofillHints: const [AutofillHints.username],
                decoration: const InputDecoration(labelText: 'Login ID', prefixIcon: Icon(Icons.person_outline_rounded)),
              ),
              const SizedBox(height: 14),
              TextField(controller: _old, obscureText: _hide, autofillHints: const [AutofillHints.password],
                  decoration: dec(widget.firstTime ? 'PIN from the slip' : 'Current password', Icons.pin_outlined)),
              const SizedBox(height: 14),
              TextField(controller: _new, obscureText: _hide, autofillHints: const [AutofillHints.newPassword],
                  decoration: dec('New password (at least 6)', Icons.lock_outline_rounded)),
              const SizedBox(height: 14),
              TextField(controller: _again, obscureText: _hide, autofillHints: const [AutofillHints.newPassword], onSubmitted: (_) => _save(),
                  decoration: dec('New password again', Icons.lock_reset_rounded)),
            ]),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: TextStyle(color: cs.error)),
          ],
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _busy ? null : _save,
            child: _busy ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white)) : const Text('Save password'),
          ),
        ]),
      ),
    );
  }
}

/// The "Account" tab: who is signed in, the school, password, sign out.
class AccountTab extends StatelessWidget {
  const AccountTab({super.key});

  Future<void> _signOut(BuildContext context) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will need your Login ID and password to sign in again.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Sign out')),
        ],
      ),
    );
    if (ok == true && context.mounted) {
      await context.read<Session>().signOut(beforeSignOut: Push.instance.beforeSignOut);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final me = s.me;
    if (me == null) return const SizedBox.shrink(); // signing out: this page is fading away
    final info = s.info;
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final modeLabel = _modes[me.mode]?.$2 ?? me.mode;
    // A parent signs in with a child's login; show the parent, not the child.
    final father = me.students.firstOrNull?.fatherName;
    final displayName = me.isParent && (father ?? '').isNotEmpty ? father! : me.name;
    return Scaffold(
      appBar: AppBar(title: const Text('Account')),
      body: ListView(padding: const EdgeInsets.fromLTRB(16, 8, 16, 32), children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Row(children: [
              InitialsAvatar(displayName, size: 60),
              const SizedBox(width: 16),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(displayName, style: t.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  const SizedBox(height: 2),
                  Text(me.staff?.designation ?? (me.isParent ? 'Parent of ${me.students.length} ${me.students.length == 1 ? 'child' : 'children'}' : me.students.firstOrNull?.classLabel ?? ''),
                      style: t.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                  const SizedBox(height: 8),
                  Wrap(spacing: 6, runSpacing: 6, children: [
                    Pill(modeLabel, color: AppColors.brand, icon: _modes[me.mode]?.$1),
                    Pill('ID ${me.loginId}', color: cs.onSurfaceVariant),
                    if (me.roleName != null) Pill(me.roleName!, color: AppColors.leave),
                  ]),
                ]),
              ),
            ]),
          ),
        ),
        const SectionTitle('School'),
        GroupCard(children: [
          ListTile(
            leading: const Icon(Icons.apartment_rounded),
            title: Text(info?.name ?? ''),
            subtitle: Text([info?.address, if ((s.school?.code ?? '').isNotEmpty) 'School Code ${s.school!.code}'].whereType<String>().join('\n')),
            isThreeLine: (info?.address ?? '').isNotEmpty,
          ),
          if ((info?.phone ?? '').isNotEmpty)
            ListTile(
              leading: const Icon(Icons.call_rounded),
              title: Text(info!.phone!),
              subtitle: const Text('Call the school office'),
              trailing: const Icon(Icons.chevron_right_rounded),
              onTap: () => launchUrl(Uri(scheme: 'tel', path: info.phone!.replaceAll(RegExp(r'[^0-9+]'), ''))),
            ),
        ]),
        const SectionTitle('Security'),
        GroupCard(children: [
          MenuTile(
            icon: Icons.password_rounded,
            color: AppColors.leave,
            title: 'Change password',
            onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const ChangePasswordScreen())),
          ),
          MenuTile(
            icon: Icons.logout_rounded,
            color: AppColors.absent,
            title: 'Sign out',
            subtitle: me.isParent ? 'To sign in as Student or Staff' : null,
            onTap: () => _signOut(context),
          ),
        ]),
        if (updateAvailable(info)) ...[
          const SectionTitle('App'),
          GroupCard(children: [
            MenuTile(
              icon: Icons.system_update_rounded,
              color: AppColors.brand,
              title: 'Update to version ${info!.latestVersion}',
              subtitle: 'You have ${Push.appVersion}',
              onTap: () => openDownloadPage(info),
            ),
          ]),
        ],
        const SizedBox(height: 28),
        Center(child: Text('EasySchool ${Push.appVersion}'.trim(), style: t.bodySmall?.copyWith(color: cs.outline))),
        Center(child: Text('by EasySol', style: t.bodySmall?.copyWith(color: cs.outline))),
      ]),
    );
  }
}
