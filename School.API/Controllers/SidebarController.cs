using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;

namespace School.API.Controllers
{
    /// <summary>
    /// The sidebar the whole application is navigated by.
    /// </summary>
    /// <remarks>
    /// Three rules this list is kept to, because breaking them is what made the
    /// previous menu hard to read:
    ///
    ///   1. A SECTION is one job, in the order the school's day runs:
    ///      admissions, teaching, exams, fees, staff, messages, documents, then
    ///      administration last — it is opened once a month, not every morning.
    ///
    ///   2. SETUP is a sub-group inside its own section, never a section of its
    ///      own. Things entered once a year do not belong beside things entered
    ///      every day.
    ///
    ///   3. A NAME says the task and, where two screens do the same thing for
    ///      different people, says who: "Class Timings (Students)" against
    ///      "Staff Timings". No two entries may be one word apart.
    ///
    /// The renderer walks this list IN THE ORDER WRITTEN and ignores `Order`
    /// entirely (_DashboardLayout.cshtml — data.menuSections.forEach). To move
    /// an entry, move the line; editing the number does nothing. `Order` is
    /// filled in to match anyway, so the two never contradict each other.
    ///
    /// `Id` must be unique across the whole menu: it is what RolePermissions
    /// will key on. The old list used 25 and 26 twice.
    ///
    /// Every page in School.Web that a person opens directly appears below.
    /// Deliberately absent: add/edit forms and print views reached from their
    /// own list page; alias routes of an entry already here (/fee/apply,
    /// /fee/ledger, /attendance/bulk, /exam/award-list); /teacher, which was
    /// only a redirect to /Employee; /student/list, a second student list that
    /// nothing links to; and the login, privacy and session-ping routes.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class SidebarController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetSidebarData()
        {
            var sidebarData = new SidebarDataDto
            {
                LogoUrl = "/images/logo.png",
                InstituteName = "SchoolMS",
                InstituteTagline = "Management System",
                MenuSections = new List<MenuSectionDto>
                {
                    // NO Dashboard section here. The layout prints its own fixed
                    // Dashboard link above this menu (#sidebarDashboardLink), so
                    // a section for it showed the word three times: the fixed
                    // link, the section heading, and the one item inside it.

                    // ── 1. Students ────────────────────────────────────────
                    // The admissions desk and the register: everything about a
                    // child, in the order it happens to them.
                    new MenuSectionDto
                    {
                        Title = "Students",
                        Icon = "bi-people-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 10, Title = "New Admission", Icon = "bi-file-earmark-plus", Url = "/admission/form", Order = 1, Section = "Students" },
                            new MenuItemDto { Id = 11, Title = "Students", Icon = "bi-person-vcard", Url = "/Student", Order = 2, Section = "Students" },
                            new MenuItemDto { Id = 12, Title = "Change Student Status", Icon = "bi-person-check-fill", Url = "/status-change", Order = 3, Section = "Students" },
                            // Three attendance screens that were all called some
                            // form of "Attendance", two of them identically.
                            new MenuItemDto { Id = 13, Title = "Mark Daily Attendance", Icon = "bi-check2-square", Url = "/attendance/form", Order = 4, Section = "Students" },
                            new MenuItemDto { Id = 17, Title = "Mark Academy Attendance", Icon = "bi-mortarboard", Url = "/attendance/academy", Order = 5, Section = "Students" },
                            new MenuItemDto { Id = 14, Title = "Attendance Record", Icon = "bi-calendar2-check-fill", Url = "/Attendance", Order = 6, Section = "Students" },
                            new MenuItemDto { Id = 15, Title = "Print Attendance Sheet", Icon = "bi-printer-fill", Url = "/attendance/sheet", Order = 7, Section = "Students" },
                            new MenuItemDto { Id = 16, Title = "Monthly Attendance Register", Icon = "bi-journal-check", Url = "/attendance/register", Order = 8, Section = "Students" }
                        }
                    },

                    // ── 2. Academics ───────────────────────────────────────
                    // What the school is made of. Subjects moved here from the
                    // Exam section: a subject is taught all year, not only sat.
                    new MenuSectionDto
                    {
                        Title = "Academics",
                        Icon = "bi-mortarboard-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 20, Title = "Campus", Icon = "bi-buildings", Url = "/Campus", Order = 1, Section = "Academics" },
                            new MenuItemDto { Id = 21, Title = "Classes", Icon = "bi-building", Url = "/Classes", Order = 2, Section = "Academics" },
                            new MenuItemDto { Id = 22, Title = "Sections", Icon = "bi-diagram-3-fill", Url = "/Sections", Order = 3, Section = "Academics" },
                            new MenuItemDto { Id = 23, Title = "Subjects", Icon = "bi-journal-bookmark-fill", Url = "/Subjects", Order = 4, Section = "Academics" },
                            // Students' own timings. Staff hours are "Staff
                            // Timings", under Staff & Payroll — separate screens
                            // for separate people, so each name says which.
                            new MenuItemDto { Id = 24, Title = "Class Timings (Students)", Icon = "bi-clock", Url = "/class-timing", Order = 5, Section = "Academics" },
                            new MenuItemDto { Id = 25, Title = "Subject Allocation", Icon = "bi-person-workspace", Url = "/staff-subjects", Order = 6, Section = "Academics" },
                            // The academy's own staff -> subject allocation and time table.
                            new MenuItemDto { Id = 31, Title = "Academy Subject Allocation", Icon = "bi-mortarboard", Url = "/academy-staff-subjects", Order = 6, Section = "Academics" },
                            // Photographed class diary, sent to parents on WhatsApp ("Today's Diary").
                            new MenuItemDto { Id = 26, Title = "Daily Diary", Icon = "bi-journal-richtext", Url = "/daily-diary", Order = 6, Section = "Academics" },
                            // Time Table (/timetable) is deliberately not listed.
                            new MenuItemDto
                            {
                                Id = 27,
                                Title = "Setup",
                                Icon = "bi-gear-fill",
                                Url = "#",
                                Order = 7,
                                Section = "Academics",
                                Children = new List<MenuItemDto>
                                {
                                    new MenuItemDto { Id = 28, Title = "Academic Sessions", Icon = "bi-calendar-range", Url = "/Sessions", Order = 1, Section = "Academics" },
                                    new MenuItemDto { Id = 29, Title = "Grading Scale", Icon = "bi-award-fill", Url = "/AcademicGrade", Order = 2, Section = "Academics" },
                                    // The LIST of statuses. Moving a child on to
                                    // one is "Change Student Status", above.
                                    new MenuItemDto { Id = 30, Title = "Student Status Types", Icon = "bi-person-check", Url = "/student-status", Order = 3, Section = "Academics" }
                                }
                            }
                        }
                    },

                    // ── 3. Examinations ────────────────────────────────────
                    new MenuSectionDto
                    {
                        Title = "Examinations",
                        Icon = "bi-clipboard2-data-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 40, Title = "Exams", Icon = "bi-clipboard2-data-fill", Url = "/Exam", Order = 1, Section = "Examinations" },
                            new MenuItemDto { Id = 41, Title = "Marks Entry", Icon = "bi-trophy-fill", Url = "/exam/marks-entry", Order = 2, Section = "Examinations" },
                            // Students left out of the teachers' result.
                            new MenuItemDto { Id = 43, Title = "Result Exclude List", Icon = "bi-person-dash-fill", Url = "/exam/exclude-list", Order = 3, Section = "Examinations" },
                            // The hub that opens all nine exam reports, the
                            // result card and the result sheet.
                            new MenuItemDto { Id = 42, Title = "Exam Reports", Icon = "bi-file-earmark-bar-graph-fill", Url = "/exam/reports", Order = 4, Section = "Examinations" }
                        }
                    },

                    // ── 4. Fees ────────────────────────────────────────────
                    // Charge it, take it, then account for it. Fee Reports and
                    // the Ledger were in a separate "Reports" section, so fee
                    // work was split across two places in the menu.
                    new MenuSectionDto
                    {
                        Title = "Fees",
                        Icon = "bi-wallet-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 50, Title = "Apply Monthly Fee", Icon = "bi-currency-dollar", Url = "/apply-fee", Order = 1, Section = "Fees" },
                            new MenuItemDto { Id = 51, Title = "Applied Fee Records", Icon = "bi-list-check", Url = "/feeList", Order = 2, Section = "Fees" },
                            new MenuItemDto { Id = 52, Title = "Receive Payment", Icon = "bi-receipt", Url = "/FeeVoucher/form", Order = 3, Section = "Fees" },
                            new MenuItemDto { Id = 53, Title = "Payment Records", Icon = "bi-journal-check", Url = "/FeeVoucher", Order = 4, Section = "Fees" },
                            // Challan Voucher (/challan) is deliberately not listed.
                            // Parents who have committed to a date. While the date
                            // holds, those children are off the unpaid list above
                            // and get no reminders.
                            new MenuItemDto { Id = 54, Title = "Promise to Pay", Icon = "bi-hand-thumbs-up", Url = "/fee/promise", Order = 5, Section = "Fees" },
                            new MenuItemDto { Id = 55, Title = "Fee Reports", Icon = "bi-cash-coin", Url = "/fee-report", Order = 6, Section = "Fees" },
                            // One entry for both statements — the page's Ledger
                            // Type radio switches between student and family.
                            new MenuItemDto { Id = 56, Title = "Student Ledger", Icon = "bi-journal-text", Url = "/ledger", Order = 7, Section = "Fees" },
                            new MenuItemDto
                            {
                                Id = 57,
                                Title = "Setup",
                                Icon = "bi-gear-fill",
                                Url = "#",
                                Order = 8,
                                Section = "Fees",
                                Children = new List<MenuItemDto>
                                {
                                    new MenuItemDto { Id = 58, Title = "Fee Types", Icon = "bi-tags-fill", Url = "/FeeType", Order = 1, Section = "Fees" },
                                    new MenuItemDto { Id = 59, Title = "Fee Structure", Icon = "bi-clipboard-data", Url = "/FeeStructure", Order = 2, Section = "Fees" },
                                    new MenuItemDto { Id = 60, Title = "Payment Methods", Icon = "bi-credit-card-fill", Url = "/PaymentMethod", Order = 3, Section = "Fees" }
                                }
                            }
                        }
                    },

                    // ── 5. Staff & Payroll ─────────────────────────────────
                    // Staff were in two sections: "Teacher" under Academic (a
                    // redirect to this same screen) and "Employee" here.
                    new MenuSectionDto
                    {
                        Title = "Staff & Payroll",
                        Icon = "bi-briefcase-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 70, Title = "Employees", Icon = "bi-person-vcard-fill", Url = "/Employee", Order = 1, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 71, Title = "Staff Attendance", Icon = "bi-calendar2-check-fill", Url = "/hr/attendance", Order = 2, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 72, Title = "Leave", Icon = "bi-calendar-minus-fill", Url = "/hr/leave", Order = 3, Section = "Staff & Payroll" },
                            // Filled in BEFORE the payroll is generated: a run reads
                            // whatever is saved there at the moment it runs.
                            new MenuItemDto { Id = 82, Title = "Monthly Amounts", Icon = "bi-cash-coin", Url = "/hr/monthly-amounts", Order = 4, Section = "Staff & Payroll" },
                            // Advances by date; their month's total reaches the payroll.
                            new MenuItemDto { Id = 87, Title = "Staff Advances", Icon = "bi-wallet2", Url = "/hr/advances", Order = 5, Section = "Staff & Payroll" },
                            // Also filled in BEFORE generating: extra days for a month with no day off.
                            new MenuItemDto { Id = 83, Title = "Extra Daily", Icon = "bi-calendar-plus-fill", Url = "/hr/extra-days", Order = 6, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 73, Title = "Generate Payroll", Icon = "bi-cash-stack", Url = "/hr/payroll", Order = 7, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 74, Title = "Staff Attendance Report", Icon = "bi-bar-chart-fill", Url = "/hr/reports/attendance", Order = 8, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 84, Title = "Attendance Card", Icon = "bi-person-lines-fill", Url = "/hr/reports/attendance-card", Order = 9, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 85, Title = "Salary Slips", Icon = "bi-receipt", Url = "/hr/reports/salary-slips", Order = 10, Section = "Staff & Payroll" },
                            new MenuItemDto { Id = 75, Title = "Staff Statement", Icon = "bi-people-fill", Url = "/hr/reports/staff-statement", Order = 11, Section = "Staff & Payroll" },
                            new MenuItemDto
                            {
                                Id = 76,
                                Title = "Setup",
                                Icon = "bi-gear-fill",
                                Url = "#",
                                Order = 11,
                                Section = "Staff & Payroll",
                                Children = new List<MenuItemDto>
                                {
                                    // The STAFF calendar. Students' hours are
                                    // "Class Timings (Students)", under Academics.
                                    new MenuItemDto { Id = 77, Title = "Staff Timings", Icon = "bi-clock-history", Url = "/hr/school-timing", Order = 1, Section = "Staff & Payroll" },
                                    // Payroll Setup carries Designations and Grades
                                    // as its first two tabs, so /designation and
                                    // /hrgrade are not listed separately.
                                    new MenuItemDto { Id = 80, Title = "Payroll Setup", Icon = "bi-sliders", Url = "/hr/setup", Order = 2, Section = "Staff & Payroll" },
                                    // The groups staff are counted and printed by.
                                    // Separate from the designation, which is the job.
                                    new MenuItemDto { Id = 81, Title = "Staff Categories", Icon = "bi-tags-fill", Url = "/staff-category", Order = 3, Section = "Staff & Payroll" },
                                    // Face-attendance machines (Hikvision) and who is on them.
                                    new MenuItemDto { Id = 86, Title = "Attendance Machines", Icon = "bi-person-bounding-box", Url = "/hr/attendance-machines", Order = 4, Section = "Staff & Payroll" }
                                }
                            }
                        }
                    },

                    // ── 6. WhatsApp ────────────────────────────────────────
                    // Called "SMS" while every message it sends is WhatsApp.
                    new MenuSectionDto
                    {
                        Title = "WhatsApp",
                        Icon = "bi-whatsapp",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 90, Title = "Send Message", Icon = "bi-send-fill", Url = "/sms", Order = 1, Section = "WhatsApp" },
                            new MenuItemDto { Id = 91, Title = "Message Log", Icon = "bi-list-ul", Url = "/sms/messages", Order = 2, Section = "WhatsApp" },
                            new MenuItemDto { Id = 92, Title = "Chat", Icon = "bi-chat-left-text-fill", Url = "/sms/chat", Order = 3, Section = "WhatsApp" },
                            // Notices given on the WhatsApp menu ("Announcements").
                            new MenuItemDto { Id = 93, Title = "Announcements", Icon = "bi-megaphone-fill", Url = "/announcements", Order = 4, Section = "WhatsApp" }
                        }
                    },

                    // ── 7. Reports & Documents ─────────────────────────────
                    // Every other report now lives with the module it reports
                    // on. These two belong to no single module.
                    new MenuSectionDto
                    {
                        Title = "Reports & Documents",
                        Icon = "bi-bar-chart-line-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 100, Title = "Certificates", Icon = "bi-award-fill", Url = "/certificates", Order = 1, Section = "Reports & Documents" },
                            // Pick your own columns and filters — students or staff.
                            new MenuItemDto { Id = 101, Title = "Custom Reports", Icon = "bi-sliders2", Url = "/custom-report", Order = 2, Section = "Reports & Documents" }
                        }
                    },

                    // ── 8. Administration ──────────────────────────────────
                    // Was the FIRST section in the menu. The office sees it
                    // every morning and opens it once a term.
                    new MenuSectionDto
                    {
                        Title = "Administration",
                        Icon = "bi-person-gear",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 110, Title = "Users", Icon = "bi-people-fill", Url = "/Users", Order = 1, Section = "Administration" },
                            new MenuItemDto { Id = 111, Title = "Roles & Permissions", Icon = "bi-shield-lock-fill", Url = "/Roles", Order = 2, Section = "Administration" },
                            new MenuItemDto { Id = 112, Title = "Page Permissions", Icon = "bi-file-earmark-text-fill", Url = "/Pages", Order = 3, Section = "Administration" },
                            new MenuItemDto { Id = 113, Title = "School Profile", Icon = "bi-building-gear", Url = "/my-institute", Order = 4, Section = "Administration" },
                            new MenuItemDto { Id = 114, Title = "Backup & Restore", Icon = "bi-cloud-arrow-up-fill", Url = "/Settings/Backup", Order = 5, Section = "Administration" },
                            new MenuItemDto { Id = 115, Title = "App Logins", Icon = "bi-phone", Url = "/app/logins", Order = 6, Section = "Administration" },
                            new MenuItemDto { Id = 117, Title = "App Usage", Icon = "bi-bar-chart-line", Url = "/app/usage", Order = 7, Section = "Administration" },
                            new MenuItemDto { Id = 116, Title = "Mobile App Permissions", Icon = "bi-phone-vibrate", Url = "/app/permissions", Order = 8, Section = "Administration" }
                        }
                    }
                }
            };

            return Ok(sidebarData);
        }
    }
}
