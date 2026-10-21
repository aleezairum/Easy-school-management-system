# EasySchool mobile app (Android)

One Flutter app for every school that runs EasySchool. On first launch the user
types the **School Code** from their login slip; the app looks it up and talks
to that school's own API (`/api/app/...`, School.API/Controllers/MobileApp).

| Who signs in | With | Sees |
|---|---|---|
| Parent | any child's Login ID + PIN, "Parent" | every enrolled child of that family |
| Student | own Login ID + PIN, "Student" | only themself |
| Staff | employee Login ID + PIN, "Staff" | own attendance, salary slips, own children; more by role (Mobile App Permissions) |

Logins are made on the web: **Administration -> App Logins** (prints the slips),
and what each staff role may do: **Administration -> Mobile App Permissions**.

## Layout

```
lib/
  main.dart              start-up, theme, which screen opens (RootGate)
  core/                  api client, session (secure storage), models, push,
                         notice_watch (notices without Firebase), school list
  ui/widgets.dart        shared pieces (loading/error/empty, tiles, cards, pictures)
  screens/               school code + sign-in, home per role, student, staff,
                         notices, PDF, update
```

## Chat (the WhatsApp menu inside the app)

The **Chat** tab is the school's WhatsApp self-service menu inside the app.
1. The user says Hi and picks from **View Options**, or types a number.
2. The answers and PDFs come from the WhatsApp bot itself:
   - POST `/api/app/chat` calls `WhatsAppBotService.ComposeReplyForAsync` with the signed-in family or staff member.
   - So the menu and the documents are identical to WhatsApp.
3. It is free: nothing goes through Meta. It works while `WhatsAppBot:Enabled` is false.

The conversation and its PDFs are kept on the phone (`lib/core/chat_store.dart`).

## Distribution: APK, no Play Store

- Parents and staff download the app from `https://school.easypos.com.pk/EasySchool/app`.
  The login slips carry a QR code to that page.
- `build-apk.ps1` builds the signed ARM APK and copies it to `School.Web/AppDownloads/`.
  Publish School.Web afterwards.
- The app reads `App:LatestVersion` and `App:MinVersion` from the API, and offers or
  forces the update. Steps: `docs/mobile-app/DEPLOY.md`.
- **Release key:** `android/key.properties` and `android/keys/easyschool-release.jks`.
  - They are NOT in git.
  - Every update must be signed with them, or it will not install over the
    parents' copy. Back them up.

## Notices on the phone

- **Without Firebase (now):** `lib/core/notice_watch.dart` checks
  `/api/app/announcements` about every 15 minutes (Android WorkManager) and
  notifies new ones.
- **With Firebase (instant):**
  1. Create a Firebase project "EasySchool" and add an Android app
     `com.easysol.easyschool`.
  2. Run:
     ```
     dart pub global activate flutterfire_cli
     flutterfire configure --project=<project-id> --platforms=android
     ```
     This replaces `lib/firebase_options.dart`.
  3. On the API, put the service-account JSON and set `Firebase:Enabled=true`.
  4. Rebuild the APK.

  The background check then switches itself off.

## Schools

`lib/core/schools.dart` maps a School Code to its API (SSMHS ->
`https://school.easypos.com.pk/api`). A new school needs one of these:
- a new line there and a new build, or
- a JSON list at `SchoolDirectory.remoteUrl`.

## Build

```
powershell -ExecutionPolicy Bypass -File build-apk.ps1     # signed release APK -> School.Web/AppDownloads
```

Testing against this PC (a debug build allows plain http):
```
flutter build apk --debug --dart-define=ES_API=http://10.0.2.2:5266/api   # Android emulator -> this PC
```
Any School Code then goes to that server.

Flutter (`%USERPROFILE%\dev\flutter`) and the JDK are not on PATH; `build-apk.ps1` sets them.

iPhone: later (needs an Apple Developer account and Codemagic).
