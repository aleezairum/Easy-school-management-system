using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.MobileApp;
using School.API.DTOs.MobileApp;
using School.API.DTOs.SMS;
using School.API.DTOs.Student;
using School.API.Repositories.Implementations.MobileApp;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;
using School.API.Services.Interfaces.Accounts;
using School.API.Services.Interfaces.Student;

namespace School.API.Services.Implementations.MobileApp
{
    /// <summary>Refused: the caller may not see or do this. The controller answers 403.</summary>
    public sealed class AppForbiddenException : Exception { public AppForbiddenException(string m) : base(m) { } }

    /// <summary>
    /// Everything the mobile app does, and who may do it. The rule is applied
    /// here, on the server, for every request - the app is never trusted to ask
    /// only for what it may see:
    ///   student - only themself;   parent - every enrolled child of the same parent row;
    ///   staff   - their own children (by their mobile), the classes they are allocated to
    ///             when their role has "View my classes", any student with "Any student", all as Admin.
    /// </summary>
    public class AppService
    {
        private readonly SchoolDbContext _db;
        private readonly AppRepository _repo;
        private readonly IWaBotRepository _bot;
        private readonly ISchoolDocuments _docs;
        private readonly ILedgerService _ledgers;
        private readonly IStdAttendenceService _attendance;
        private readonly AppTokenIssuer _tokens;
        private readonly School.API.Services.Interfaces.HR.IPayrollService _payroll;
        private readonly IAnnouncementService _announcements;
        private readonly IConfiguration _config;

        public AppService(SchoolDbContext db, AppRepository repo, IWaBotRepository bot, ISchoolDocuments docs,
            ILedgerService ledgers, IStdAttendenceService attendance, AppTokenIssuer tokens,
            School.API.Services.Interfaces.HR.IPayrollService payroll, IAnnouncementService announcements, IConfiguration config)
        {
            _db = db; _repo = repo; _bot = bot; _docs = docs; _ledgers = ledgers; _attendance = attendance; _tokens = tokens; _payroll = payroll;
            _announcements = announcements; _config = config;
        }

        // ── sign in ───────────────────────────────────────────────────

        private const int MaxFailed = 5;

        public async Task<(AppLoginResultDto? Result, string? Error)> LoginAsync(AppLoginDto dto)
        {
            var mode = (dto.Mode ?? "").Trim().ToLowerInvariant();
            if (mode is not ("student" or "parent" or "staff")) return (null, "Choose Student, Parent or Staff.");
            var loginId = (dto.LoginId ?? "").Trim();
            if (loginId.Length == 0 || string.IsNullOrEmpty(dto.Password)) return (null, "Enter your login ID and password.");

            var type = mode == "staff" ? AppAccountType.Staff : AppAccountType.Student;
            var acc = await _db.AppAccounts.FirstOrDefaultAsync(a => a.AccountType == type && a.LoginId == loginId);
            const string wrong = "The login ID or password is not correct.";
            if (acc == null) return (null, wrong);
            if (!acc.IsActive) return (null, "This login has been switched off. Please contact the school office.");
            if (acc.LockedUntil > DateTime.UtcNow)
                return (null, $"Too many wrong passwords. Try again after {Math.Ceiling((acc.LockedUntil.Value - DateTime.UtcNow).TotalMinutes)} minute(s).");

            if (!AppPassword.Verify(dto.Password, acc.PasswordHash, acc.PasswordSalt))
            {
                acc.FailedLogins++;
                if (acc.FailedLogins >= MaxFailed) { acc.LockedUntil = DateTime.UtcNow.AddMinutes(15); acc.FailedLogins = 0; }
                await _db.SaveChangesAsync();
                return (null, wrong);
            }

            // A child who has left the school no longer has a login.
            if (acc.StudentId.HasValue && (await _repo.GetStudentAsync(acc.StudentId.Value)) is not { Enrolled: true })
                return (null, "This student is no longer enrolled. Please contact the school office.");

            acc.FailedLogins = 0; acc.LockedUntil = null; acc.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var (token, expires) = _tokens.Issue(acc, mode);
            var caller = await new AppCallerResolver(_db).ResolveAsync(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(AppClaims.Account, acc.Id.ToString()),
                    new System.Security.Claims.Claim(AppClaims.Mode, mode)
                })));
            return (new AppLoginResultDto { Token = token, ExpiresAt = expires, MustChangePassword = acc.MustChangePassword, Me = await MeAsync(caller!) }, null);
        }

        public async Task<string?> ChangePasswordAsync(AppCaller caller, AppChangePasswordDto dto)
        {
            var acc = await _db.AppAccounts.FirstAsync(a => a.Id == caller.AccountId);
            if (!AppPassword.Verify(dto.OldPassword ?? "", acc.PasswordHash, acc.PasswordSalt)) return "The current password is not correct.";
            var pw = dto.NewPassword ?? "";
            if (pw.Length < 6) return "Use at least 6 characters.";
            if (pw == dto.OldPassword) return "Choose a password different from the current one.";
            (acc.PasswordHash, acc.PasswordSalt) = AppPassword.Hash(pw);
            acc.InitialPassword = null;
            acc.MustChangePassword = false;
            acc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return null;
        }

        public async Task<AppMeDto> MeAsync(AppCaller c)
        {
            var me = new AppMeDto
            {
                AccountId = c.AccountId, Mode = c.Mode, Name = c.Name, LoginId = c.LoginId, RoleName = c.RoleName, IsAdmin = c.IsAdmin,
                Features = c.IsAdmin ? AppFeatures.All.Select(f => f.Key).ToList() : c.Features.ToList(),
                Students = await VisibleStudentsAsync(c)
            };
            if (c.EmployeeId.HasValue) me.Staff = (await _repo.GetStaffAsync(c.EmployeeId.Value))?.Card;
            // Same audience rule as AnnouncementsAsync: staff mode gets staff notices, the rest parents'.
            var prefix = AppTopics.Prefix(_config);
            me.Topics = c.IsStaff
                ? AppTopics.ForStaff(prefix, me.Staff?.CampusId)
                : AppTopics.ForParent(prefix, me.Students.Select(s => s.CampusId));
            return me;
        }

        public AppSchoolInfoDto SchoolInfo() => new()
        {
            Name = _config["WhatsAppBot:SchoolName"] ?? "School",
            Address = _config["WhatsAppBot:Address"],
            Phone = _config["WhatsAppBot:OfficePhone"],
            LogoUrl = _config["App:LogoUrl"],
            SchoolCode = _config["App:SchoolCode"],
            MinAppVersion = _config["App:MinVersion"] ?? "1.0.0",
            LatestVersion = _config["App:LatestVersion"] ?? _config["App:MinVersion"] ?? "1.0.0",
            DownloadUrl = _config["App:DownloadUrl"]
        };

        // ── scope ─────────────────────────────────────────────────────

        /// <summary>The children shown on the caller's home screen.</summary>
        public async Task<List<AppStudentCardDto>> VisibleStudentsAsync(AppCaller c)
        {
            if (c.Mode == "student" && c.StudentId.HasValue)
                return (await _repo.GetStudentAsync(c.StudentId.Value)) is { Enrolled: true } s ? new() { s.Card } : new();
            if (c.Mode == "parent")
            {
                if (c.ParentId.HasValue) return await _repo.GetFamilyAsync(c.ParentId.Value);
                if (c.StudentId.HasValue && (await _repo.GetStudentAsync(c.StudentId.Value)) is { Enrolled: true } only) return new() { only.Card };
                return new();
            }
            if (c.EmployeeId.HasValue)
                return await _repo.GetChildrenByMobileAsync((await _repo.GetStaffAsync(c.EmployeeId.Value))?.Mobile);
            return new();
        }

        /// <summary>The student, if the caller may see them; otherwise 403.</summary>
        public async Task<(AppStudentCardDto Card, int? ParentId)> StudentAsync(AppCaller c, int studentId)
        {
            var s = await _repo.GetStudentAsync(studentId);
            if (s is null) throw new KeyNotFoundException();
            var (card, enrolled, parentId) = s.Value;
            var ok = c.Mode switch
            {
                "student" => c.StudentId == studentId,
                "parent" => enrolled && (c.StudentId == studentId || (c.ParentId.HasValue && parentId == c.ParentId)),
                "staff" => c.Can(AppFeatures.AdminStudents)
                           || (await VisibleStudentsAsync(c)).Any(x => x.StudentId == studentId)
                           || (c.Can(AppFeatures.ClassView) && c.EmployeeId.HasValue
                               && (await _repo.GetTeacherClassesAsync(c.EmployeeId.Value)).Any(k => k.SectionId == card.SectionId)),
                _ => false
            };
            if (!ok) throw new AppForbiddenException("You cannot see this student.");
            return (card, parentId);
        }

        internal static BotStudentDto Bot(AppStudentCardDto s, int? parentId) => new()
        {
            StudentId = s.StudentId, ParentId = parentId, Name = s.Name, RollNo = s.RollNo, CampusId = s.CampusId,
            ClassId = s.ClassId, SectionId = s.SectionId, ClassName = s.ClassName, SectionName = s.SectionName, FatherName = s.FatherName
        };

        // ── a student ─────────────────────────────────────────────────

        private static string Code(int code, bool late) => code switch { 1 => late ? "T" : "P", 2 => "L", _ => "A" };

        public async Task<AppStudentSummaryDto> StudentSummaryAsync(AppCaller c, int studentId)
        {
            var (card, _) = await StudentAsync(c, studentId);
            var today = DateTime.Today;
            var marks = await _repo.GetStudentAttendanceAsync(studentId, new DateTime(today.Year, today.Month, 1), today);
            var todayMark = marks.FirstOrDefault(m => m.Date.Date == today);
            var dues = await _bot.GetPendingDuesAsync(studentId);
            var exams = await _repo.GetStudentExamsAsync(studentId);
            var diary = await _repo.GetDiaryAsync(card.ClassId, card.SectionId, today, today);
            int p = marks.Count(m => m.Code == 1), a = marks.Count(m => m.Code == 0), l = marks.Count(m => m.Code == 2);
            return new AppStudentSummaryDto
            {
                Student = card,
                TodayStatus = todayMark == default ? null : Code(todayMark.Code, todayMark.Late),
                MonthPresent = p, MonthAbsent = a, MonthLeave = l,
                MonthPercent = p + a + l > 0 ? Math.Round(p * 100m / (p + a + l), 1) : null,
                DuesTotal = dues.Sum(d => d.PendingAmount), DuesMonths = dues.Select(d => d.VDate.ToString("yyyyMM")).Distinct().Count(),
                LastExamName = exams.FirstOrDefault()?.ExamName, LastExamPercent = exams.FirstOrDefault()?.Percent,
                DiaryPagesToday = diary.Sum(d => Math.Max(1, d.ImageIds.Count))
            };
        }

        public async Task<AppAttendanceMonthDto> AttendanceMonthAsync(AppCaller c, int studentId, int year, int month)
        {
            await StudentAsync(c, studentId);
            var from = new DateTime(year, month, 1); var to = from.AddMonths(1).AddDays(-1);
            var marks = (await _repo.GetStudentAttendanceAsync(studentId, from, to)).ToDictionary(m => m.Date.Date);
            var holidays = await _repo.GetHolidaysAsync(from, to);
            var res = new AppAttendanceMonthDto { Year = year, Month = month };
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                string status = ""; string? note = null;
                if (marks.TryGetValue(d, out var m)) { status = Code(m.Code, m.Late); note = m.Remarks; }
                else if (holidays.TryGetValue(d, out var h)) { status = "H"; note = h; }
                else if (d.DayOfWeek == DayOfWeek.Sunday) status = "S";
                res.Days.Add(new AppAttendanceDayDto { Date = d, Status = status, Note = note });
            }
            res.Present = res.Days.Count(x => x.Status is "P" or "T"); res.Late = res.Days.Count(x => x.Status == "T");
            res.Absent = res.Days.Count(x => x.Status == "A"); res.Leave = res.Days.Count(x => x.Status == "L");
            var marked = res.Present + res.Absent + res.Leave;
            res.Percent = marked > 0 ? Math.Round(res.Present * 100m / marked, 1) : null;
            return res;
        }

        public async Task<AppFeesDto> FeesAsync(AppCaller c, int studentId)
        {
            await StudentAsync(c, studentId);
            var dues = await _bot.GetPendingDuesAsync(studentId);
            var v = await _bot.GetLastVoucherAsync(studentId);
            decimal? balance = null;
            try { balance = (await _ledgers.GetStudentLedgerAsync(studentId, null, null)).ClosingBalance; } catch { }
            return new AppFeesDto
            {
                DuesTotal = dues.Sum(d => d.PendingAmount),
                Dues = dues.Select(d => new AppDueDto { Month = d.VDate, FeeType = d.FeeTypeName, Amount = d.PendingAmount }).ToList(),
                LastVoucher = v == null ? null : new AppVoucherDto { VoucherNo = v.VNo, Date = v.VDate, Amount = v.PaidAmount, PaymentMethod = v.PaymentMethod },
                LedgerBalance = balance
            };
        }

        public async Task<List<AppLedgerRowDto>> LedgerAsync(AppCaller c, int studentId)
        {
            await StudentAsync(c, studentId);
            var l = await _ledgers.GetStudentLedgerAsync(studentId, null, null);
            return l.Rows.Where(r => r.RowType == 1).Select(r => new AppLedgerRowDto
            {
                Date = r.LineDate ?? DateTime.MinValue, Description = r.Description, Debit = r.Debit, Credit = r.Credit, Balance = r.Balance
            }).ToList();
        }

        public async Task<BotOutgoing?> VoucherPdfAsync(AppCaller c, int studentId)
        {
            var (card, pid) = await StudentAsync(c, studentId);
            return (await _docs.VoucherPdfsAsync(new() { Bot(card, pid) })).FirstOrDefault(d => d.HasDocument);
        }

        public async Task<BotOutgoing?> LedgerPdfAsync(AppCaller c, int studentId)
        {
            var (card, pid) = await StudentAsync(c, studentId);
            // only this child's statement (a parent asking for the family ledger gets it from the family screen)
            return (await _docs.LedgerPdfsAsync(new() { Bot(card, null) })).FirstOrDefault(d => d.HasDocument);
        }

        public async Task<List<AppExamDto>> ExamsAsync(AppCaller c, int studentId)
        {
            await StudentAsync(c, studentId);
            return await _repo.GetStudentExamsAsync(studentId);
        }

        public async Task<List<AppExamSubjectDto>> ExamSubjectsAsync(AppCaller c, int studentId, int examId)
        {
            await StudentAsync(c, studentId);
            return await _repo.GetStudentExamSubjectsAsync(studentId, examId);
        }

        public async Task<BotOutgoing?> ResultCardPdfAsync(AppCaller c, int studentId, int examId)
        {
            var (card, pid) = await StudentAsync(c, studentId);
            var exam = (await _repo.GetStudentExamsAsync(studentId)).FirstOrDefault(e => e.ExamId == examId);
            if (exam == null) return null;                 // not finalised, or no marks
            return await _docs.ResultCardPdfAsync(Bot(card, pid), examId, exam.ExamName);
        }

        public async Task<List<AppDiaryDto>> DiaryAsync(AppCaller c, int studentId, DateTime from, DateTime to)
        {
            var (card, _) = await StudentAsync(c, studentId);
            return await _repo.GetDiaryAsync(card.ClassId, card.SectionId, from, to);
        }

        /// <summary>A diary picture, when it belongs to a class/section the caller may see.</summary>
        public async Task<(byte[] Data, string ContentType)?> DiaryImageAsync(AppCaller c, int imageId)
        {
            var img = await _repo.GetDiaryImageAsync(imageId);
            if (img is null) return null;
            var (data, type, classId, sectionId) = img.Value;
            var students = await VisibleStudentsAsync(c);
            var ok = students.Any(s => s.ClassId == classId && (sectionId == null || s.SectionId == sectionId))
                     || c.Can(AppFeatures.AdminStudents)
                     || (c.EmployeeId.HasValue && c.Can(AppFeatures.ClassView)
                         && (await _repo.GetTeacherClassesAsync(c.EmployeeId.Value)).Any(k => k.ClassId == classId && (sectionId == null || k.SectionId == sectionId)));
            if (!ok) throw new AppForbiddenException("You cannot see this diary.");
            return (data, type);
        }

        public async Task<AppTimingDto?> TimingAsync(AppCaller c, int studentId)
        {
            var (card, _) = await StudentAsync(c, studentId);
            var t = (await _bot.GetClassTimingsAsync()).FirstOrDefault(x => x.CampusId == card.CampusId && x.ClassId == card.ClassId);
            static string Clock(TimeSpan v) => DateTime.Today.Add(v).ToString("hh:mm tt");
            return t == null ? null : new AppTimingDto
            {
                TimeIn = Clock(t.TimeIn), TimeOut = Clock(t.TimeOut),
                FridayIn = t.FridayTimeIn.HasValue ? Clock(t.FridayTimeIn.Value) : null,
                FridayOut = t.FridayTimeOut.HasValue ? Clock(t.FridayTimeOut.Value) : null
            };
        }

        public async Task<List<AppAnnouncementDto>> AnnouncementsAsync(AppCaller c)
        {
            if (c.IsStaff)
            {
                var staff = c.EmployeeId.HasValue ? (await _repo.GetStaffAsync(c.EmployeeId.Value))?.Card : null;
                return await _repo.GetAnnouncementsAsync(2, staff?.CampusId is int ci ? new[] { ci } : Array.Empty<int>());
            }
            return await _repo.GetAnnouncementsAsync(1, (await VisibleStudentsAsync(c)).Select(s => s.CampusId));
        }

        /// <summary>
        /// "Send announcements": saved like one from the web (so it shows there
        /// too, and on WhatsApp) and pushed at once. Starts today. Anyone but an
        /// admin can address only their own campus.
        /// </summary>
        public async Task<AppAnnouncementDto> PostAnnouncementAsync(AppCaller c, AppPostAnnouncementDto dto)
        {
            if (!c.IsStaff || !c.Can(AppFeatures.AnnounceSend)) throw new AppForbiddenException("Your role cannot send announcements.");
            int? campus = dto.CampusId is > 0 ? dto.CampusId : null;
            if (!c.IsAdmin)
            {
                var own = c.EmployeeId.HasValue ? (await _repo.GetStaffAsync(c.EmployeeId.Value))?.Card.CampusId : null;
                campus = own ?? campus;
            }
            var saved = await _announcements.SaveAsync(null, new SaveAnnouncementDto
            {
                Title = dto.Title, Message = dto.Message, StartDate = DateTime.Today, EndDate = dto.EndDate,
                Audience = dto.Audience, CampusId = campus, IsActive = true, Image = dto.Image
            }, null);
            return new AppAnnouncementDto
            {
                Id = saved.Id, Title = saved.Title, Message = saved.Message, StartDate = saved.StartDate, EndDate = saved.EndDate, HasImage = saved.HasImage
            };
        }

        public async Task<(byte[] Data, string ContentType)?> AnnouncementImageAsync(AppCaller c, int id)
        {
            if (!(await AnnouncementsAsync(c)).Any(a => a.Id == id)) throw new AppForbiddenException("Not your announcement.");
            return await _repo.GetAnnouncementImageAsync(id);
        }

        // ── staff ─────────────────────────────────────────────────────

        private async Task<(AppStaffCardDto Card, string? Mobile)> StaffSelfAsync(AppCaller c)
        {
            if (!c.EmployeeId.HasValue) throw new AppForbiddenException("This login is not linked to a member of staff.");
            return await _repo.GetStaffAsync(c.EmployeeId.Value) ?? throw new KeyNotFoundException();
        }

        internal static BotStaffDto BotStaff(AppStaffCardDto s) => new()
        {
            EmployeeId = s.EmployeeId, EmployeeCode = s.EmployeeCode ?? "", Name = s.Name, DesignationName = s.Designation,
            CampusId = s.CampusId, CampusName = s.CampusName
        };

        public async Task<AppStaffSummaryDto> StaffSummaryAsync(AppCaller c)
        {
            var (card, _) = await StaffSelfAsync(c);
            var today = DateTime.Today;
            var days = await _repo.GetStaffDaysAsync(card.EmployeeId, new DateTime(today.Year, today.Month, 1), today);
            var t = days.FirstOrDefault(d => d.Date.Date == today);
            var slips = await PayslipsAsync(c, 3);
            return new AppStaffSummaryDto
            {
                Staff = card, TodayStatus = t?.Status, TodayIn = t?.TimeIn, TodayOut = t?.TimeOut,
                MonthPresent = days.Count(d => d.Status is "Present" or "Half Day" or "Short Leave" or "Exempt"),
                MonthAbsent = days.Count(d => d.Status == "Absent"), MonthLeave = days.Count(d => d.Status == "Leave"),
                MonthLateMinutes = days.Sum(d => d.LateMinutes),
                LastPayslip = slips.FirstOrDefault(),
                ClassCount = c.Can(AppFeatures.ClassView) ? (await _repo.GetTeacherClassesAsync(card.EmployeeId)).Count : 0
            };
        }

        public async Task<List<AppStaffDayDto>> StaffMonthAsync(AppCaller c, int year, int month)
        {
            var (card, _) = await StaffSelfAsync(c);
            var from = new DateTime(year, month, 1);
            return await _repo.GetStaffDaysAsync(card.EmployeeId, from, from.AddMonths(1).AddDays(-1));
        }

        public async Task<List<AppPayslipDto>> PayslipsAsync(AppCaller c, int months = 12)
        {
            var (card, _) = await StaffSelfAsync(c);
            var to = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);
            var from = to.AddDays(1).AddMonths(-months);
            var slips = await _payroll.GetPayslipsForPeriodAsync(from, to, null, null, card.EmployeeId);
            return slips.Select(p => new AppPayslipDto
            {
                Year = p.PayYear, Month = p.PayMonth, Gross = p.GrossEarning, Deduction = p.TotalDeduction,
                TakeHome = Math.Round(p.TakeHome, 0, MidpointRounding.AwayFromZero),
                IsFinal = p.RunStatus != (byte)School.API.Data.DBModels.HR.HrPayrollRunStatus.Draft, PaidDate = p.PaidDate
            }).OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();
        }

        public async Task<(BotOutgoing? Doc, string? Note)> StaffDocumentAsync(AppCaller c, string kind, int year, int month)
        {
            var (card, _) = await StaffSelfAsync(c);
            var m = new DateTime(year, month, 1);
            return kind == "slip" ? await _docs.SalarySlipPdfAsync(BotStaff(card), m) : await _docs.AttendanceCardPdfAsync(BotStaff(card), m);
        }

        public async Task<List<AppClassDto>> ClassesAsync(AppCaller c)
        {
            if (!c.Can(AppFeatures.ClassView)) throw new AppForbiddenException("Your role cannot see classes in the app.");
            var (card, _) = await StaffSelfAsync(c);
            var classes = await _repo.GetTeacherClassesAsync(card.EmployeeId);
            foreach (var k in classes) k.Students = (await _repo.GetSectionStudentsAsync(k.SectionId)).Count;
            return classes;
        }

        /// <summary>A section the caller teaches (or any, as Admin), for the class screens.</summary>
        private async Task EnsureSectionAsync(AppCaller c, int sectionId, string feature)
        {
            if (!c.Can(feature)) throw new AppForbiddenException("Your role cannot do this in the app.");
            if (c.IsAdmin) return;
            if (!c.EmployeeId.HasValue || !(await _repo.GetTeacherClassesAsync(c.EmployeeId.Value)).Any(k => k.SectionId == sectionId))
                throw new AppForbiddenException("This is not one of your classes.");
        }

        public async Task<AppClassAttendanceDto> ClassAttendanceAsync(AppCaller c, int sectionId, DateTime date)
        {
            await EnsureSectionAsync(c, sectionId, AppFeatures.ClassView);
            var students = await _repo.GetSectionStudentsAsync(sectionId);
            var marks = (await _repo.GetSectionDayAsync(sectionId, date)).ToDictionary(m => m.StudentId);
            return new AppClassAttendanceDto
            {
                Date = date.Date, Marked = marks.Count > 0,
                Rows = students.Select(s => new AppClassAttendanceRowDto
                {
                    StudentId = s.StudentId, RollNo = s.RollNo, Name = s.Name, FatherName = s.FatherName,
                    Status = marks.TryGetValue(s.StudentId, out var m) && m.Code.HasValue ? Code(m.Code.Value, m.Late) : ""
                }).ToList()
            };
        }

        public async Task<int> SaveClassAttendanceAsync(AppCaller c, int sectionId, AppSaveClassAttendanceDto dto, int userId, string ip)
        {
            await EnsureSectionAsync(c, sectionId, AppFeatures.ClassAttendance);
            if (dto.Date.Date > DateTime.Today) throw new AppForbiddenException("Attendance cannot be marked for a future date.");
            var allowed = (await _repo.GetSectionStudentsAsync(sectionId)).Select(s => s.StudentId).ToHashSet();
            var sessionId = await _repo.GetCurrentSessionIdAsync() ?? 0;
            var saved = 0;
            foreach (var r in dto.Rows.Where(r => allowed.Contains(r.StudentId) && r.Status is "P" or "T" or "A" or "L"))
            {
                await _attendance.SaveAsync(new StdAttendenceSaveDto
                {
                    SessionID = sessionId, StudentID = r.StudentId, SectionID = sectionId, VDate = dto.Date.Date,
                    Attendence = r.Status switch { "A" => 0, "L" => 2, _ => 1 }, IsLate = r.Status == "T",
                    Remarks = "App"
                }, userId, ip);
                saved++;
            }
            return saved;
        }

        // ── admin ─────────────────────────────────────────────────────

        public async Task<List<AppStudentCardDto>> SearchStudentsAsync(AppCaller c, string term)
        {
            if (!c.Can(AppFeatures.AdminStudents)) throw new AppForbiddenException("Your role cannot search students.");
            term = (term ?? "").Trim();
            return term.Length < 2 ? new() : await _repo.SearchStudentsAsync(term, 30);
        }

        // ── devices (push) ────────────────────────────────────────────

        public async Task RegisterDeviceAsync(AppCaller c, AppDeviceDto dto)
        {
            var token = (dto.Token ?? "").Trim();
            if (token.Length < 20) return;
            var d = await _db.AppDevices.FirstOrDefaultAsync(x => x.Token == token);
            if (d == null) _db.AppDevices.Add(d = new AppDevice { Token = token });
            d.AccountId = c.AccountId; d.Mode = c.Mode; d.Platform = dto.Platform; d.AppVersion = dto.AppVersion; d.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task RemoveDeviceAsync(AppCaller c, string token)
        {
            var d = await _db.AppDevices.FirstOrDefaultAsync(x => x.Token == token && x.AccountId == c.AccountId);
            if (d != null) { _db.AppDevices.Remove(d); await _db.SaveChangesAsync(); }
        }
    }
}
