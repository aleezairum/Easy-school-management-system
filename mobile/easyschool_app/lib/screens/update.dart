import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/models.dart';
import '../core/push.dart';
import '../core/session.dart';
import '../main.dart';

/// The app is not in the Play Store: a new version is an APK on the school's
/// download page (api App:LatestVersion / App:DownloadUrl).

bool updateAvailable(SchoolInfo? info) =>
    info != null && Push.appVersion.isNotEmpty && isNewerVersion(info.latestVersion, Push.appVersion);

bool updateRequired(SchoolInfo? info) =>
    info != null && Push.appVersion.isNotEmpty && isNewerVersion(info.minAppVersion, Push.appVersion);

Future<void> openDownloadPage(SchoolInfo? info) async {
  final url = info?.downloadUrl;
  if (url == null || url.isEmpty) return;
  await launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
}

bool _offered = false;

/// Once per launch, on the home screen.
Future<void> offerUpdate(BuildContext context) async {
  final info = context.read<Session>().info;
  if (_offered || !updateAvailable(info)) return;
  _offered = true;
  final go = await showDialog<bool>(
    context: context,
    builder: (c) => AlertDialog(
      icon: const Icon(Icons.system_update_rounded, size: 36),
      title: const Text('New version available'),
      content: Text('EasySchool ${info!.latestVersion} is ready (you have ${Push.appVersion}). '
          'Download it from the school\'s page and install it over this one - you stay signed in.'),
      actions: [
        TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Later')),
        FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Update')),
      ],
    ),
  );
  if (go == true) await openDownloadPage(info);
}

/// This version no longer works with the school's server.
class UpdateRequiredScreen extends StatelessWidget {
  const UpdateRequiredScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final info = context.watch<Session>().info;
    final t = Theme.of(context).textTheme;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(28),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              const BrandMark(size: 76),
              const SizedBox(height: 24),
              Text('Please update EasySchool', textAlign: TextAlign.center, style: t.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(height: 10),
              Text('This version (${Push.appVersion}) is too old for ${info?.name ?? 'the school'}. '
                  'Download the new one (${info?.latestVersion}) and install it over this one.',
                  textAlign: TextAlign.center, style: t.bodyLarge),
              const SizedBox(height: 28),
              FilledButton.icon(
                onPressed: () => openDownloadPage(info),
                icon: const Icon(Icons.download_rounded),
                label: const Text('Download the new version'),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
