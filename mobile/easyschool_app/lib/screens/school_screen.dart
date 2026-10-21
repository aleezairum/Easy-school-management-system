import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api.dart';
import '../core/schools.dart';
import '../core/session.dart';
import '../core/theme.dart';
import '../main.dart';
import '../ui/widgets.dart';

/// First launch: the School Code printed on the login slip finds the school's server.
class SchoolScreen extends StatefulWidget {
  const SchoolScreen({super.key});

  @override
  State<SchoolScreen> createState() => _SchoolScreenState();
}

class _SchoolScreenState extends State<SchoolScreen> {
  final _code = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _go({SchoolEntry? manual}) async {
    final session = context.read<Session>();
    final code = _code.text.trim();
    if (manual == null && code.isEmpty) {
      setState(() => _error = 'Enter the School Code from your login slip.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final entry = manual ?? await SchoolDirectory.find(code);
      if (entry == null) {
        setState(() => _error = 'No school with the code "${code.toUpperCase()}". Check the code on your login slip.');
        return;
      }
      final info = await session.probe(entry);
      await session.chooseSchool(entry, info);
    } on ApiException catch (e) {
      setState(() => _error = e.status == 0 ? e.message : 'The school server did not answer properly. Please try again later.');
    } catch (_) {
      setState(() => _error = 'Could not reach the school. Please try again.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _manual() async {
    final ctl = TextEditingController();
    final url = await showDialog<String>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Server address'),
        content: TextField(
          controller: ctl,
          autofocus: true,
          keyboardType: TextInputType.url,
          decoration: const InputDecoration(hintText: 'https://school.example.com'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(c, ctl.text), child: const Text('Connect')),
        ],
      ),
    );
    if (url != null && url.trim().isNotEmpty) _go(manual: SchoolEntry.manual(url));
  }

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                const Center(child: BrandMark(size: 84)),
                const SizedBox(height: 24),
                Text('Welcome to EasySchool', textAlign: TextAlign.center, style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
                const SizedBox(height: 8),
                Text('Attendance, fees, results and school notices - all in one place.',
                    textAlign: TextAlign.center, style: t.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 36),
                TextField(
                  controller: _code,
                  textCapitalization: TextCapitalization.characters,
                  textInputAction: TextInputAction.go,
                  onSubmitted: (_) => _go(),
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, letterSpacing: 2),
                  decoration: InputDecoration(
                    labelText: 'School Code',
                    hintText: 'e.g. SSMHS',
                    prefixIcon: const Icon(Icons.apartment_rounded),
                    errorText: _error,
                    errorMaxLines: 3,
                  ),
                ),
                const SizedBox(height: 8),
                Text('It is printed on the login slip the school gave you.', style: t.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: _busy ? null : _go,
                  child: _busy ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white)) : const Text('Continue'),
                ),
                const SizedBox(height: 12),
                TextButton(onPressed: _busy ? null : _manual, child: const Text('School not listed? Enter server address')),
                const SizedBox(height: 32),
                Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                  Icon(Icons.lock_outline_rounded, size: 14, color: cs.outline),
                  const SizedBox(width: 6),
                  Text('Your data stays with your school', style: t.bodySmall?.copyWith(color: cs.outline)),
                ]),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}

/// Signed in, but the phone could not reach the school when the app opened.
class OfflineScreen extends StatefulWidget {
  const OfflineScreen({super.key});

  @override
  State<OfflineScreen> createState() => _OfflineScreenState();
}

class _OfflineScreenState extends State<OfflineScreen> {
  bool _busy = false;

  Future<void> _retry() async {
    setState(() => _busy = true);
    try {
      await context.read<Session>().refreshMe();
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SafeArea(
          child: _busy
              ? const Center(child: CircularProgressIndicator())
              : ErrorView(message: 'Could not reach ${context.read<Session>().info?.name ?? 'the school'}.\nCheck your internet connection.', onRetry: _retry),
        ),
        bottomNavigationBar: SafeArea(
          child: TextButton(
            onPressed: () => context.read<Session>().signOut(),
            child: const Text('Sign out', style: TextStyle(color: AppColors.absent)),
          ),
        ),
      );
}
