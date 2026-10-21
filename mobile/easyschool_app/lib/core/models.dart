// The API's answers (School.API/DTOs/MobileApp/AppDtos.cs), read from JSON.

DateTime? _date(dynamic v) => v == null ? null : DateTime.tryParse(v.toString());
double _num(dynamic v) => v == null ? 0 : (v as num).toDouble();
double? _numOrNull(dynamic v) => v == null ? null : (v as num).toDouble();
List<T> _list<T>(dynamic v, T Function(Map<String, dynamic>) f) =>
    v is List ? v.map((e) => f(e as Map<String, dynamic>)).toList() : <T>[];

/// Feature keys a staff role can be given (AppFeatures on the server).
class Feature {
  static const classView = 'app.class.view';
  static const classAttendance = 'app.class.attendance';
  static const classDiary = 'app.class.diary';
  static const classMarks = 'app.class.marks';
  static const adminDashboard = 'app.admin.dashboard';
  static const adminStudents = 'app.admin.students';
  static const adminStaff = 'app.admin.staff';
  static const announceSend = 'app.announce.send';
}

class SchoolInfo {
  SchoolInfo.fromJson(Map<String, dynamic> j)
      : name = j['name'] ?? 'School',
        address = j['address'],
        phone = j['phone'],
        logoUrl = (j['logoUrl'] as String?)?.isNotEmpty == true ? j['logoUrl'] : null,
        schoolCode = j['schoolCode'],
        minAppVersion = j['minAppVersion'] ?? '1.0.0',
        latestVersion = j['latestVersion'] ?? j['minAppVersion'] ?? '1.0.0',
        downloadUrl = j['downloadUrl'];
  final String name;
  final String? address, phone, logoUrl, schoolCode, downloadUrl;
  final String minAppVersion, latestVersion;

  Map<String, dynamic> toJson() => {
        'name': name, 'address': address, 'phone': phone, 'logoUrl': logoUrl,
        'schoolCode': schoolCode, 'minAppVersion': minAppVersion,
        'latestVersion': latestVersion, 'downloadUrl': downloadUrl,
      };
}

class StudentCard {
  StudentCard.fromJson(Map<String, dynamic> j)
      : studentId = j['studentId'],
        name = j['name'] ?? '',
        rollNo = j['rollNo'],
        fatherName = j['fatherName'],
        campusId = j['campusId'] ?? 0,
        campusName = j['campusName'],
        classId = j['classId'] ?? 0,
        className = j['className'],
        sectionId = j['sectionId'] ?? 0,
        sectionName = j['sectionName'],
        gender = j['gender'];
  final int studentId, campusId, classId, sectionId;
  final String name;
  final String? rollNo, fatherName, campusName, className, sectionName, gender;

  /// "Class 7 - B2"
  String get classLabel {
    final c = className ?? '';
    final cls = RegExp(r'^\d+$').hasMatch(c) ? 'Class $c' : c;
    return [cls, if ((sectionName ?? '').isNotEmpty) sectionName].where((e) => e!.isNotEmpty).join(' - ');
  }

  bool get isGirl => gender == '0' || gender?.toLowerCase() == 'female';
}

class StaffCard {
  StaffCard.fromJson(Map<String, dynamic> j)
      : employeeId = j['employeeId'],
        employeeCode = j['employeeCode'],
        name = j['name'] ?? '',
        designation = j['designation'],
        campusId = j['campusId'],
        campusName = j['campusName'],
        hasPhoto = j['hasPhoto'] ?? false;
  final int employeeId;
  final int? campusId;
  final String name;
  final String? employeeCode, designation, campusName;
  final bool hasPhoto;
}

class Me {
  Me.fromJson(Map<String, dynamic> j)
      : accountId = j['accountId'],
        mode = j['mode'] ?? 'parent',
        name = j['name'] ?? '',
        loginId = j['loginId'] ?? '',
        roleName = j['roleName'],
        isAdmin = j['isAdmin'] ?? false,
        features = ((j['features'] as List?) ?? []).cast<String>().toSet(),
        students = _list(j['students'], StudentCard.fromJson),
        staff = j['staff'] == null ? null : StaffCard.fromJson(j['staff']),
        topics = ((j['topics'] as List?) ?? []).cast<String>();
  final int accountId;
  final String mode, name, loginId;
  final String? roleName;
  final bool isAdmin;
  final Set<String> features;
  final List<StudentCard> students;
  final StaffCard? staff;
  final List<String> topics;

  bool get isStaff => mode == 'staff';
  bool get isParent => mode == 'parent';
  bool get isStudent => mode == 'student';
  bool can(String feature) => isAdmin || features.contains(feature);
}

class StudentSummary {
  StudentSummary.fromJson(Map<String, dynamic> j)
      : student = StudentCard.fromJson(j['student']),
        todayStatus = j['todayStatus'],
        monthPresent = j['monthPresent'] ?? 0,
        monthAbsent = j['monthAbsent'] ?? 0,
        monthLeave = j['monthLeave'] ?? 0,
        monthPercent = _numOrNull(j['monthPercent']),
        duesTotal = _num(j['duesTotal']),
        duesMonths = j['duesMonths'] ?? 0,
        lastExamName = j['lastExamName'],
        lastExamPercent = _numOrNull(j['lastExamPercent']),
        diaryPagesToday = j['diaryPagesToday'] ?? 0;
  final StudentCard student;
  final String? todayStatus, lastExamName;
  final int monthPresent, monthAbsent, monthLeave, duesMonths, diaryPagesToday;
  final double? monthPercent, lastExamPercent;
  final double duesTotal;
}

class AttendanceDay {
  AttendanceDay.fromJson(Map<String, dynamic> j)
      : date = _date(j['date'])!,
        status = j['status'] ?? '',
        note = j['note'];
  final DateTime date;

  /// P, T (late), A, L; H holiday; S Sunday; "" not marked.
  final String status;
  final String? note;
}

class AttendanceMonth {
  AttendanceMonth.fromJson(Map<String, dynamic> j)
      : year = j['year'],
        month = j['month'],
        days = _list(j['days'], AttendanceDay.fromJson),
        present = j['present'] ?? 0,
        late = j['late'] ?? 0,
        absent = j['absent'] ?? 0,
        leave = j['leave'] ?? 0,
        percent = _numOrNull(j['percent']);
  final int year, month, present, late, absent, leave;
  final List<AttendanceDay> days;
  final double? percent;
}

class FeeDue {
  FeeDue.fromJson(Map<String, dynamic> j)
      : month = _date(j['month'])!,
        feeType = j['feeType'],
        amount = _num(j['amount']);
  final DateTime month;
  final String? feeType;
  final double amount;
}

class Voucher {
  Voucher.fromJson(Map<String, dynamic> j)
      : voucherNo = j['voucherNo'],
        date = _date(j['date'])!,
        amount = _num(j['amount']),
        paymentMethod = j['paymentMethod'];
  final String? voucherNo, paymentMethod;
  final DateTime date;
  final double amount;
}

class Fees {
  Fees.fromJson(Map<String, dynamic> j)
      : duesTotal = _num(j['duesTotal']),
        dues = _list(j['dues'], FeeDue.fromJson),
        lastVoucher = j['lastVoucher'] == null ? null : Voucher.fromJson(j['lastVoucher']),
        ledgerBalance = _numOrNull(j['ledgerBalance']);
  final double duesTotal;
  final List<FeeDue> dues;
  final Voucher? lastVoucher;
  final double? ledgerBalance;
}

class LedgerRow {
  LedgerRow.fromJson(Map<String, dynamic> j)
      : date = _date(j['date'])!,
        description = j['description'],
        debit = _num(j['debit']),
        credit = _num(j['credit']),
        balance = _num(j['balance']);
  final DateTime date;
  final String? description;
  final double debit, credit, balance;
}

class Exam {
  Exam.fromJson(Map<String, dynamic> j)
      : examId = j['examId'],
        examName = j['examName'] ?? '',
        totalMarks = _num(j['totalMarks']),
        obtainedMarks = _num(j['obtainedMarks']),
        percent = _numOrNull(j['percent']),
        failedSubjects = j['failedSubjects'] ?? 0,
        firstPaper = _date(j['firstPaper']);
  final int examId, failedSubjects;
  final String examName;
  final double totalMarks, obtainedMarks;
  final double? percent;
  final DateTime? firstPaper;
}

class ExamSubject {
  ExamSubject.fromJson(Map<String, dynamic> j)
      : subjectName = (j['subjectName'] as String? ?? '').replaceAll(RegExp(r'\s+'), ' ').trim(),
        date = _date(j['date']),
        totalMarks = _num(j['totalMarks']),
        passMarks = _num(j['passMarks']),
        obtained = _numOrNull(j['obtained']),
        absent = j['absent'] ?? false,
        passed = j['passed'] ?? false;
  final String subjectName;
  final DateTime? date;
  final double totalMarks, passMarks;
  final double? obtained;
  final bool absent, passed;
}

class DiaryPage {
  DiaryPage.fromJson(Map<String, dynamic> j)
      : diaryId = j['diaryId'],
        date = _date(j['date'])!,
        notes = j['notes'],
        imageIds = ((j['imageIds'] as List?) ?? []).cast<int>();
  final int diaryId;
  final DateTime date;
  final String? notes;
  final List<int> imageIds;
}

class Timing {
  Timing.fromJson(Map<String, dynamic> j)
      : timeIn = j['timeIn'] ?? '',
        timeOut = j['timeOut'] ?? '',
        fridayIn = j['fridayIn'],
        fridayOut = j['fridayOut'];
  final String timeIn, timeOut;
  final String? fridayIn, fridayOut;
}

class Announcement {
  Announcement.fromJson(Map<String, dynamic> j)
      : id = j['id'],
        title = j['title'] ?? '',
        message = j['message'] ?? '',
        startDate = _date(j['startDate'])!,
        endDate = _date(j['endDate']),
        hasImage = j['hasImage'] ?? false;
  final int id;
  final String title, message;
  final DateTime startDate;
  final DateTime? endDate;
  final bool hasImage;
}

class Payslip {
  Payslip.fromJson(Map<String, dynamic> j)
      : year = j['year'],
        month = j['month'],
        gross = _num(j['gross']),
        deduction = _num(j['deduction']),
        takeHome = _num(j['takeHome']),
        isFinal = j['isFinal'] ?? false,
        paidDate = _date(j['paidDate']);
  final int year, month;
  final double gross, deduction, takeHome;
  final bool isFinal;
  final DateTime? paidDate;
}

class StaffSummary {
  StaffSummary.fromJson(Map<String, dynamic> j)
      : staff = StaffCard.fromJson(j['staff']),
        todayStatus = j['todayStatus'],
        todayIn = j['todayIn'],
        todayOut = j['todayOut'],
        monthPresent = j['monthPresent'] ?? 0,
        monthAbsent = j['monthAbsent'] ?? 0,
        monthLeave = j['monthLeave'] ?? 0,
        monthLateMinutes = j['monthLateMinutes'] ?? 0,
        lastPayslip = j['lastPayslip'] == null ? null : Payslip.fromJson(j['lastPayslip']),
        classCount = j['classCount'] ?? 0;
  final StaffCard staff;
  final String? todayStatus, todayIn, todayOut;
  final int monthPresent, monthAbsent, monthLeave, monthLateMinutes, classCount;
  final Payslip? lastPayslip;
}

class StaffDay {
  StaffDay.fromJson(Map<String, dynamic> j)
      : date = _date(j['date'])!,
        status = j['status'] ?? '',
        timeIn = j['timeIn'],
        timeOut = j['timeOut'],
        lateMinutes = j['lateMinutes'] ?? 0,
        earlyMinutes = j['earlyMinutes'] ?? 0;
  final DateTime date;
  final String status;
  final String? timeIn, timeOut;
  final int lateMinutes, earlyMinutes;
}

class TeacherClass {
  TeacherClass.fromJson(Map<String, dynamic> j)
      : classId = j['classId'],
        className = j['className'],
        sectionId = j['sectionId'],
        sectionName = j['sectionName'],
        subjects = ((j['subjects'] as List?) ?? []).cast<String>().map((s) => s.replaceAll(RegExp(r'\s+'), ' ').trim()).toList(),
        students = j['students'] ?? 0;
  final int classId, sectionId, students;
  final String? className, sectionName;
  final List<String> subjects;

  String get label {
    final c = className ?? '';
    return '${RegExp(r'^\d+$').hasMatch(c) ? 'Class $c' : c} - ${sectionName ?? ''}';
  }
}

class ClassAttendanceRow {
  ClassAttendanceRow.fromJson(Map<String, dynamic> j)
      : studentId = j['studentId'],
        rollNo = j['rollNo'],
        name = j['name'] ?? '',
        fatherName = j['fatherName'],
        status = j['status'] ?? '';
  final int studentId;
  final String? rollNo, fatherName;
  final String name;
  String status;
}

class ClassAttendance {
  ClassAttendance.fromJson(Map<String, dynamic> j)
      : date = _date(j['date'])!,
        marked = j['marked'] ?? false,
        rows = _list(j['rows'], ClassAttendanceRow.fromJson);
  final DateTime date;
  final bool marked;
  final List<ClassAttendanceRow> rows;
}

/// 1.2.3 > 1.2 ; true when [a] is a later version than [b].
bool isNewerVersion(String a, String b) {
  List<int> p(String v) => v.split(RegExp(r'[.+-]')).map((x) => int.tryParse(x) ?? 0).toList();
  final x = p(a), y = p(b);
  for (var i = 0; i < 3; i++) {
    final d = (i < x.length ? x[i] : 0) - (i < y.length ? y[i] : 0);
    if (d != 0) return d > 0;
  }
  return false;
}
