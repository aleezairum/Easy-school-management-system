using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;

namespace School.API.Controllers
{
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
                    // 1. User Management
                    new MenuSectionDto
                    {
                        Title = "User Management",
                        Icon = "bi-person-gear",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 1, Title = "Pages", Icon = "bi-file-earmark-text-fill", Url = "/Pages", Order = 1, Section = "User Management" },
                            new MenuItemDto { Id = 2, Title = "Roles", Icon = "bi-shield-lock-fill", Url = "/Roles", Order = 2, Section = "User Management" },
                            new MenuItemDto { Id = 3, Title = "Users", Icon = "bi-people-fill", Url = "/Users", Order = 3, Section = "User Management" }
                        }
                    },
                    // 2. Academic
                    new MenuSectionDto
                    {
                        Title = "Academic",
                        Icon = "bi-mortarboard-fill",
                        Items = new List<MenuItemDto>
                        {
                            // 2.1 Setups (with children)
                            new MenuItemDto
                            {
                                Id = 4,
                                Title = "Setups",
                                Icon = "bi-gear-fill",
                                Url = "#",
                                Order = 1,
                                Section = "Academic",
                                Children = new List<MenuItemDto>
                                {
                                    new MenuItemDto { Id = 5, Title = "Academic Session Year", Icon = "bi-calendar-range", Url = "/Sessions", Order = 1, Section = "Academic" },
                                    new MenuItemDto { Id = 6, Title = "Academic Grade", Icon = "bi-award-fill", Url = "/AcademicGrade", Order = 2, Section = "Academic" },
                                    new MenuItemDto { Id = 7, Title = "Section", Icon = "bi-diagram-3-fill", Url = "/Sections", Order = 3, Section = "Academic" },
                                    new MenuItemDto { Id = 8, Title = "Class", Icon = "bi-building", Url = "/Classes", Order = 4, Section = "Academic" },
                                    new MenuItemDto { Id = 9, Title = "Subject", Icon = "bi-journal-bookmark-fill", Url = "/Subjects", Order = 5, Section = "Academic" },
                                    new MenuItemDto { Id = 10, Title = "Time Table", Icon = "bi-calendar-event-fill", Url = "/TimeTable", Order = 6, Section = "Academic" },
                                    new MenuItemDto { Id = 20, Title = "Staff Subjects", Icon = "bi-person-workspace", Url = "/staff-subjects", Order = 7, Section = "Academic" }
                                }
                            },
                            // 2.2 Transactions (with children)
                            new MenuItemDto
                            {
                                Id = 11,
                                Title = "Transactions",
                                Icon = "bi-arrow-left-right",
                                Url = "#",
                                Order = 2,
                                Section = "Academic",
                                Children = new List<MenuItemDto>
                                {
                                    new MenuItemDto { Id = 12, Title = "Student", Icon = "bi-person-badge-fill", Url = "/Student", Order = 1, Section = "Academic" },
                                    new MenuItemDto { Id = 13, Title = "Attendance", Icon = "bi-calendar2-check-fill", Url = "/Attendance", Order = 2, Section = "Academic" },
                                    new MenuItemDto { Id = 21, Title = "Mark Attendance", Icon = "bi-check2-square", Url = "/attendance/form", Order = 3, Section = "Academic" },
                                    new MenuItemDto { Id = 14, Title = "Exams", Icon = "bi-clipboard2-data-fill", Url = "/Exam", Order = 4, Section = "Academic" },
                                    new MenuItemDto { Id = 22, Title = "Award List / Marks", Icon = "bi-trophy-fill", Url = "/exam/marks-entry", Order = 5, Section = "Academic" }
                                }
                            }
                        }
                    },
                    // 3. HR Payroll
                    new MenuSectionDto
                    {
                        Title = "HR Payroll",
                        Icon = "bi-briefcase-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 15, Title = "Employee", Icon = "bi-person-vcard-fill", Url = "/Employee", Order = 1, Section = "HR Payroll" },
                            new MenuItemDto { Id = 16, Title = "Salary", Icon = "bi-cash-stack", Url = "/Salary", Order = 2, Section = "HR Payroll" }
                        }
                    },
                    // 4. Fee Management
                    new MenuSectionDto
                    {
                        Title = "Fee Management",
                        Icon = "bi-wallet-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 17, Title = "Payment Method", Icon = "bi-credit-card-fill", Url = "/PaymentMethod", Order = 1, Section = "Fee Management" },
                            new MenuItemDto { Id = 18, Title = "Challan Voucher", Icon = "bi-receipt-cutoff", Url = "/ChallanVoucher", Order = 2, Section = "Fee Management" },
                            new MenuItemDto { Id = 19, Title = "Fee Voucher", Icon = "bi-receipt", Url = "/FeeVoucher", Order = 3, Section = "Fee Management" },
                            new MenuItemDto { Id = 23, Title = "Apply Fee", Icon = "bi-currency-dollar", Url = "/apply-fee", Order = 4, Section = "Fee Management" }
                        }
                    },
                    // 5. SMS Module
                    new MenuSectionDto
                    {
                        Title = "SMS",
                        Icon = "bi-envelope-fill",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 24, Title = "SMS Module", Icon = "bi-chat-dots-fill", Url = "/sms", Order = 1, Section = "SMS" }
                        }
                    }
                }
            };

            return Ok(sidebarData);
        }
    }
}
