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
            // TEMPORARY MOCK DATA (replace when DB is ready)

            var sidebarData = new SidebarDataDto
            {
                LogoUrl = "/images/logo.png",
                InstituteName = "SchoolMS",
                InstituteTagline = "Management System",
                MenuSections = new List<MenuSectionDto>
                {
                    new MenuSectionDto
                    {
                        Title = "Main Menu",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 1, Title = "Dashboard", Icon = "bi-grid-1x2-fill", Url = "/Dashboard", Order = 1, Section = "Main Menu" }
                        }
                    },
                    new MenuSectionDto
                    {
                        Title = "Administration",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 14, Title = "User Management", Icon = "bi-person-fill", Url = "/Users", Order = 1, Section = "Administration" },
                            new MenuItemDto { Id = 15, Title = "Role Permissions", Icon = "bi-shield-lock-fill", Url = "/Roles", Order = 2, Section = "Administration" },
                            new MenuItemDto { Id = 16, Title = "Page Management", Icon = "bi-file-earmark-text-fill", Url = "/Pages", Order = 3, Section = "Administration" }
                        }
                    },
                    new MenuSectionDto
                    {
                        Title = "Management",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 2, Title = "Students", Icon = "bi-people-fill", Url = "/Students", Badge = "1.2k", Order = 1, Section = "Management" },
                            new MenuItemDto { Id = 3, Title = "Teachers", Icon = "bi-person-badge-fill", Url = "/Teachers", Order = 2, Section = "Management" },
                            new MenuItemDto { Id = 4, Title = "Classes", Icon = "bi-building", Url = "/Classes", Order = 3, Section = "Management" },
                            new MenuItemDto { Id = 5, Title = "Subjects", Icon = "bi-journal-bookmark-fill", Url = "/Subjects", Order = 4, Section = "Management" }
                        }
                    },
                    new MenuSectionDto
                    {
                        Title = "Academic",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 6, Title = "Attendance", Icon = "bi-calendar2-check-fill", Url = "/Attendance", Order = 1, Section = "Academic" },
                            new MenuItemDto { Id = 7, Title = "Examinations", Icon = "bi-clipboard2-data-fill", Url = "/Examinations", Order = 2, Section = "Academic" },
                            new MenuItemDto { Id = 8, Title = "Results", Icon = "bi-file-earmark-bar-graph-fill", Url = "/Results", Order = 3, Section = "Academic" },
                            new MenuItemDto { Id = 9, Title = "Timetable", Icon = "bi-calendar-event-fill", Url = "/Timetable", Order = 4, Section = "Academic" }
                        }
                    },
                    new MenuSectionDto
                    {
                        Title = "Finance",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 10, Title = "Fee Management", Icon = "bi-credit-card-fill", Url = "/Fees", Order = 1, Section = "Finance" },
                            new MenuItemDto { Id = 11, Title = "Invoices", Icon = "bi-receipt", Url = "/Invoices", Order = 2, Section = "Finance" }
                        }
                    },
                    new MenuSectionDto
                    {
                        Title = "Settings",
                        Items = new List<MenuItemDto>
                        {
                            new MenuItemDto { Id = 12, Title = "Settings", Icon = "bi-gear-fill", Url = "/Settings", Order = 1, Section = "Settings" },
                            new MenuItemDto { Id = 13, Title = "Logout", Icon = "bi-box-arrow-left", Url = "/Account/Logout", Order = 2, Section = "Settings" }
                        }
                    }
                }
            };

            return Ok(sidebarData);
        }
    }
}
