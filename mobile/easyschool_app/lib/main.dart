import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:provider/provider.dart';

import 'core/push.dart';
import 'core/session.dart';
import 'core/theme.dart';
import 'screens/account_screens.dart';
import 'screens/home_shell.dart';
import 'screens/school_screen.dart';
import 'screens/update.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);
  try {
    Push.appVersion = (await PackageInfo.fromPlatform()).version;
  } catch (_) {}

  final session = Session();
  session.onMeChanged = (me) => Push.instance.sync(me);
  await Push.instance.init(session);
  runApp(ChangeNotifierProvider.value(value: session, child: const EasySchoolApp()));
  session.load();
}

class EasySchoolApp extends StatelessWidget {
  const EasySchoolApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'EasySchool',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(Brightness.light),
        darkTheme: buildTheme(Brightness.dark),
        themeMode: ThemeMode.light,
        home: const RootGate(),
      );
}

/// Which screen the app opens on: school code, sign-in, new password, or home.
class RootGate extends StatelessWidget {
  const RootGate({super.key});

  @override
  Widget build(BuildContext context) {
    final s = context.watch<Session>();
    final Widget page;
    if (!s.ready) {
      page = const SplashScreen();
    } else if (!s.hasSchool) {
      page = const SchoolScreen();
    } else if (updateRequired(s.info)) {
      page = const UpdateRequiredScreen();
    } else if (s.token == null) {
      page = const LoginScreen();
    } else if (s.me == null) {
      page = const OfflineScreen();
    } else if (s.mustChangePassword) {
      page = const ChangePasswordScreen(firstTime: true);
    } else {
      page = HomeShell(key: ValueKey('${s.me!.accountId}-${s.me!.mode}'));
    }
    return AnimatedSwitcher(duration: const Duration(milliseconds: 250), child: page);
  }
}

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
        backgroundColor: AppColors.brand,
        body: Center(child: BrandMark(size: 96, light: true)),
      );
}

/// The EasySchool mark: a cap on a rounded square.
class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 72, this.light = false});
  final double size;
  final bool light;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: light ? Colors.white : AppColors.brand,
          borderRadius: BorderRadius.circular(size * 0.28),
          boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.12), blurRadius: 18, offset: const Offset(0, 6))],
        ),
        child: Icon(Icons.school_rounded, size: size * 0.58, color: light ? AppColors.brand : Colors.white),
      );
}
