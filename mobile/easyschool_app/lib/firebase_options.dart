// PLACEHOLDER until the EasySchool Firebase project exists.
//
// Then, from mobile/easyschool_app:
//   dart pub global activate flutterfire_cli
//   flutterfire configure --project=<firebase-project-id> --platforms=android,ios
// which replaces this file with the real keys. Until then push notifications
// are off and the rest of the app works normally.
import 'package:firebase_core/firebase_core.dart';

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform =>
      throw UnsupportedError('Firebase is not configured yet (run flutterfire configure).');
}
