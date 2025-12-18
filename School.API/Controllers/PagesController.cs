using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagesController : ControllerBase
    {
        // Mock data storage (replace with DB later)
        private static List<PageDto> _pages = new List<PageDto>
        {
            // Dashboard
            new PageDto { Id = 1, SrNo = 1, PageName = "Dashboard", Url = "/Dashboard", RoutingPageUrl = "/Dashboard/Index", Icon = "bi-grid-1x2-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 1 },

            // Student Management
            new PageDto { Id = 2, SrNo = 2, PageName = "Students", Url = "/Students", RoutingPageUrl = "/Students/Index", Icon = "bi-people-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 2 },
            new PageDto { Id = 3, SrNo = 3, PageName = "Student List", Url = "/Students/List", RoutingPageUrl = "/Students/List", Icon = "bi-list-ul", MenuPage = "Students", IsActive = true, ParentId = 2, DisplayOrder = 1 },
            new PageDto { Id = 4, SrNo = 4, PageName = "Add Student", Url = "/Students/Add", RoutingPageUrl = "/Students/Add", Icon = "bi-person-plus", MenuPage = "Students", IsActive = true, ParentId = 2, DisplayOrder = 2 },
            new PageDto { Id = 5, SrNo = 5, PageName = "Student Attendance", Url = "/Students/Attendance", RoutingPageUrl = "/Students/Attendance", Icon = "bi-calendar-check", MenuPage = "Students", IsActive = true, ParentId = 2, DisplayOrder = 3 },

            // Teacher Management
            new PageDto { Id = 6, SrNo = 6, PageName = "Teachers", Url = "/Teachers", RoutingPageUrl = "/Teachers/Index", Icon = "bi-person-badge-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 3 },
            new PageDto { Id = 7, SrNo = 7, PageName = "Teacher List", Url = "/Teachers/List", RoutingPageUrl = "/Teachers/List", Icon = "bi-list-ul", MenuPage = "Teachers", IsActive = true, ParentId = 6, DisplayOrder = 1 },
            new PageDto { Id = 8, SrNo = 8, PageName = "Add Teacher", Url = "/Teachers/Add", RoutingPageUrl = "/Teachers/Add", Icon = "bi-person-plus", MenuPage = "Teachers", IsActive = true, ParentId = 6, DisplayOrder = 2 },

            // Classes & Sections
            new PageDto { Id = 9, SrNo = 9, PageName = "Classes", Url = "/Classes", RoutingPageUrl = "/Classes/Index", Icon = "bi-building", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 4 },
            new PageDto { Id = 10, SrNo = 10, PageName = "Class List", Url = "/Classes/List", RoutingPageUrl = "/Classes/List", Icon = "bi-list-ul", MenuPage = "Classes", IsActive = true, ParentId = 9, DisplayOrder = 1 },
            new PageDto { Id = 11, SrNo = 11, PageName = "Sections", Url = "/Classes/Sections", RoutingPageUrl = "/Classes/Sections", Icon = "bi-grid", MenuPage = "Classes", IsActive = true, ParentId = 9, DisplayOrder = 2 },

            // Subjects
            new PageDto { Id = 12, SrNo = 12, PageName = "Subjects", Url = "/Subjects", RoutingPageUrl = "/Subjects/Index", Icon = "bi-journal-bookmark-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 5 },

            // Examinations
            new PageDto { Id = 13, SrNo = 13, PageName = "Examinations", Url = "/Examinations", RoutingPageUrl = "/Examinations/Index", Icon = "bi-clipboard2-data-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 6 },
            new PageDto { Id = 14, SrNo = 14, PageName = "Exam Schedule", Url = "/Examinations/Schedule", RoutingPageUrl = "/Examinations/Schedule", Icon = "bi-calendar-event", MenuPage = "Examinations", IsActive = true, ParentId = 13, DisplayOrder = 1 },
            new PageDto { Id = 15, SrNo = 15, PageName = "Results", Url = "/Examinations/Results", RoutingPageUrl = "/Examinations/Results", Icon = "bi-file-earmark-bar-graph", MenuPage = "Examinations", IsActive = true, ParentId = 13, DisplayOrder = 2 },

            // Fee Management
            new PageDto { Id = 16, SrNo = 16, PageName = "Fee Management", Url = "/Fees", RoutingPageUrl = "/Fees/Index", Icon = "bi-credit-card-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 7 },
            new PageDto { Id = 17, SrNo = 17, PageName = "Fee Structure", Url = "/Fees/Structure", RoutingPageUrl = "/Fees/Structure", Icon = "bi-list-check", MenuPage = "Fee Management", IsActive = true, ParentId = 16, DisplayOrder = 1 },
            new PageDto { Id = 18, SrNo = 18, PageName = "Fee Collection", Url = "/Fees/Collection", RoutingPageUrl = "/Fees/Collection", Icon = "bi-cash-stack", MenuPage = "Fee Management", IsActive = true, ParentId = 16, DisplayOrder = 2 },

            // Settings
            new PageDto { Id = 19, SrNo = 19, PageName = "Settings", Url = "/Settings", RoutingPageUrl = "/Settings/Index", Icon = "bi-gear-fill", MenuPage = "Main", IsActive = true, ParentId = null, DisplayOrder = 8 },
            new PageDto { Id = 20, SrNo = 20, PageName = "User Management", Url = "/Settings/Users", RoutingPageUrl = "/Settings/Users", Icon = "bi-people", MenuPage = "Settings", IsActive = true, ParentId = 19, DisplayOrder = 1 },
            new PageDto { Id = 21, SrNo = 21, PageName = "Role Management", Url = "/Settings/Roles", RoutingPageUrl = "/Settings/Roles", Icon = "bi-shield-check", MenuPage = "Settings", IsActive = true, ParentId = 19, DisplayOrder = 2 },
            new PageDto { Id = 22, SrNo = 22, PageName = "Page Management", Url = "/Pages", RoutingPageUrl = "/Pages/Index", Icon = "bi-file-earmark-plus", MenuPage = "Settings", IsActive = true, ParentId = 19, DisplayOrder = 3 }
        };

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_pages.OrderBy(p => p.DisplayOrder));
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var page = _pages.FirstOrDefault(p => p.Id == id);
            if (page == null)
                return NotFound(new { message = "Page not found" });

            return Ok(page);
        }

        [HttpGet("tree")]
        public IActionResult GetTree()
        {
            var treeItems = BuildTree(null);
            return Ok(treeItems);
        }

        [HttpGet("next-srno")]
        public IActionResult GetNextSrNo()
        {
            var nextSrNo = _pages.Any() ? _pages.Max(p => p.SrNo) + 1 : 1;
            return Ok(new { srNo = nextSrNo });
        }

        [HttpPost]
        public IActionResult Create([FromBody] PageDto dto)
        {
            if (string.IsNullOrEmpty(dto.PageName))
                return BadRequest(new { message = "Page name is required" });

            dto.Id = _pages.Any() ? _pages.Max(p => p.Id) + 1 : 1;
            dto.SrNo = _pages.Any() ? _pages.Max(p => p.SrNo) + 1 : 1;

            _pages.Add(dto);

            return Ok(new { message = "Page created successfully", page = dto });
        }

        [HttpPost("sibling/{parentId}")]
        public IActionResult CreateSibling(int parentId, [FromBody] PageDto dto)
        {
            var parentPage = _pages.FirstOrDefault(p => p.Id == parentId);
            if (parentPage == null)
                return NotFound(new { message = "Parent page not found" });

            dto.Id = _pages.Any() ? _pages.Max(p => p.Id) + 1 : 1;
            dto.SrNo = _pages.Any() ? _pages.Max(p => p.SrNo) + 1 : 1;
            dto.ParentId = parentPage.ParentId; // Same parent as the selected page

            _pages.Add(dto);

            return Ok(new { message = "Sibling page created successfully", page = dto });
        }

        [HttpPost("child/{parentId}")]
        public IActionResult CreateChild(int parentId, [FromBody] PageDto dto)
        {
            var parentPage = _pages.FirstOrDefault(p => p.Id == parentId);
            if (parentPage == null)
                return NotFound(new { message = "Parent page not found" });

            dto.Id = _pages.Any() ? _pages.Max(p => p.Id) + 1 : 1;
            dto.SrNo = _pages.Any() ? _pages.Max(p => p.SrNo) + 1 : 1;
            dto.ParentId = parentId; // Selected page becomes parent

            _pages.Add(dto);

            return Ok(new { message = "Child page created successfully", page = dto });
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] PageDto dto)
        {
            var page = _pages.FirstOrDefault(p => p.Id == id);
            if (page == null)
                return NotFound(new { message = "Page not found" });

            page.PageName = dto.PageName;
            page.Url = dto.Url;
            page.RoutingPageUrl = dto.RoutingPageUrl;
            page.Icon = dto.Icon;
            page.MenuPage = dto.MenuPage;
            page.IsActive = dto.IsActive;
            page.ParentId = dto.ParentId;
            page.DisplayOrder = dto.DisplayOrder;

            return Ok(new { message = "Page updated successfully", page });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var page = _pages.FirstOrDefault(p => p.Id == id);
            if (page == null)
                return NotFound(new { message = "Page not found" });

            // Check if page has children
            var hasChildren = _pages.Any(p => p.ParentId == id);
            if (hasChildren)
                return BadRequest(new { message = "Cannot delete page with children. Delete children first." });

            _pages.Remove(page);

            return Ok(new { message = "Page deleted successfully" });
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
                return Ok(_pages);

            var results = _pages.Where(p =>
                p.PageName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Url.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.MenuPage.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return Ok(results);
        }

        private List<PageTreeItemDto> BuildTree(int? parentId)
        {
            return _pages
                .Where(p => p.ParentId == parentId)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new PageTreeItemDto
                {
                    Id = p.Id,
                    Name = p.PageName,
                    Icon = p.Icon,
                    ParentId = p.ParentId,
                    IsExpanded = true,
                    Children = BuildTree(p.Id)
                })
                .ToList();
        }
    }
}
