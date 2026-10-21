using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    /// <summary>
    /// The Page Permissions form (/pages): the list of pages the application
    /// has, which Roles &amp; Permissions then grants rights against.
    /// </summary>
    /// <remarks>
    /// This used to be a `static List&lt;PageDto&gt;` held in memory. Two
    /// consequences: every page added or edited was lost when the application
    /// restarted (and was shared by every user until then), and the list it
    /// started from described a different application — "Teacher Management",
    /// "Examinations > Schedule", "Email Settings" — none of which exist here.
    ///
    /// It now reads and writes the Menus table, the same rows the Roles form
    /// grants permissions on, so the two forms can no longer disagree about
    /// what a page is.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class PagesController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public PagesController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var menus = await OrderedAsync();
            return Ok(ToDtos(menus));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var menus = await OrderedAsync();
            var page = menus.FirstOrDefault(m => m.Id == id);

            if (page == null)
                return NotFound(new { message = "Page not found" });

            return Ok(ToDto(page, menus));
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetTree()
        {
            var menus = await OrderedAsync();
            return Ok(BuildTree(menus, null));
        }

        [HttpGet("next-srno")]
        public async Task<IActionResult> GetNextSrNo()
        {
            var max = await _context.Menus.MaxAsync(m => (int?)m.SrNo) ?? 0;
            return Ok(new { srNo = max + 1 });
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] PageDto dto) => CreateUnder(dto, dto.ParentId);

        [HttpPost("sibling/{parentId}")]
        public async Task<IActionResult> CreateSibling(int parentId, [FromBody] PageDto dto)
        {
            // "Sibling of the selected page" — so it takes the selected page's
            // own parent, not the selected page.
            var selected = await _context.Menus.FindAsync(parentId);
            if (selected == null) return NotFound(new { message = "Parent page not found" });

            return await CreateUnder(dto, selected.ParentId);
        }

        [HttpPost("child/{parentId}")]
        public async Task<IActionResult> CreateChild(int parentId, [FromBody] PageDto dto)
        {
            var parent = await _context.Menus.FindAsync(parentId);
            if (parent == null) return NotFound(new { message = "Parent page not found" });

            return await CreateUnder(dto, parentId);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PageDto dto)
        {
            var page = await _context.Menus.FindAsync(id);
            if (page == null) return NotFound(new { message = "Page not found" });

            if (string.IsNullOrWhiteSpace(dto.PageName))
                return BadRequest(new { message = "Page name is required" });

            // A page cannot be its own parent, nor sit under one of its own
            // descendants — either makes the tree unprintable and the sidebar
            // recursion never terminate.
            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId == id)
                    return BadRequest(new { message = "A page cannot be its own parent." });

                var all = await _context.Menus.AsNoTracking().ToListAsync();
                if (IsDescendant(all, dto.ParentId.Value, id))
                    return BadRequest(new { message = "That parent sits underneath this page." });
            }

            page.MenuTitle      = dto.PageName.Trim();
            page.Url            = Clean(dto.Url);
            page.RoutingPageUrl = Clean(dto.RoutingPageUrl);
            page.Icon           = Clean(dto.Icon);
            page.IsActive       = dto.IsActive;
            page.ParentId       = dto.ParentId;
            page.DisplayOrder   = dto.DisplayOrder;
            page.Level          = dto.ParentId.HasValue ? 2 : 1;
            page.UpdatedAt      = DateTime.Now;

            await _context.SaveChangesAsync();

            var menus = await OrderedAsync();
            return Ok(new { message = "Page updated successfully", page = ToDto(page, menus) });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var page = await _context.Menus.FindAsync(id);
            if (page == null) return NotFound(new { message = "Page not found" });

            if (await _context.Menus.AnyAsync(m => m.ParentId == id))
                return BadRequest(new { message = "Cannot delete page with children. Delete children first." });

            // The rights granted on this page go with it. Left behind they
            // would be invisible to every form and would be inherited by
            // whatever page took the id next.
            var permissions = await _context.RolePermissions.Where(p => p.MenuId == id).ToListAsync();
            if (permissions.Count > 0) _context.RolePermissions.RemoveRange(permissions);

            _context.Menus.Remove(page);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = permissions.Count > 0
                    ? $"Page deleted, along with {permissions.Count} permission(s) granted on it."
                    : "Page deleted successfully"
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var menus = await OrderedAsync();
            if (string.IsNullOrWhiteSpace(query)) return Ok(ToDtos(menus));

            var q = query.Trim();
            var hits = menus.Where(m =>
                    m.MenuTitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (m.Url ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    SectionOf(m, menus).Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(ToDtos(hits, menus));
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private async Task<IActionResult> CreateUnder(PageDto dto, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(dto.PageName))
                return BadRequest(new { message = "Page name is required" });

            var maxSrNo = await _context.Menus.MaxAsync(m => (int?)m.SrNo) ?? 0;

            // Put it last within its own parent unless the form gave an order.
            var order = dto.DisplayOrder > 0
                ? dto.DisplayOrder
                : (await _context.Menus.Where(m => m.ParentId == parentId)
                                       .MaxAsync(m => (int?)m.DisplayOrder) ?? 0) + 1;

            var page = new Menu
            {
                MenuTitle      = dto.PageName.Trim(),
                Url            = Clean(dto.Url),
                RoutingPageUrl = Clean(dto.RoutingPageUrl),
                Icon           = Clean(dto.Icon),
                ParentId       = parentId,
                DisplayOrder   = order,
                IsActive       = dto.IsActive,
                Level          = parentId.HasValue ? 2 : 1,
                SrNo           = maxSrNo + 1,
                CreatedAt      = DateTime.Now
            };

            _context.Menus.Add(page);
            await _context.SaveChangesAsync();

            var menus = await OrderedAsync();
            return Ok(new { message = "Page created successfully", page = ToDto(page, menus) });
        }

        private Task<List<Menu>> OrderedAsync() =>
            _context.Menus.AsNoTracking()
                    .OrderBy(m => m.ParentId == null ? m.DisplayOrder : 0)
                    .ThenBy(m => m.ParentId)
                    .ThenBy(m => m.DisplayOrder)
                    .ToListAsync();

        // Walks up from `candidate`: true when `id` is one of its ancestors,
        // which would make `candidate` a descendant of the page being moved.
        private static bool IsDescendant(List<Menu> all, int candidate, int id)
        {
            var seen = new HashSet<int>();
            int? walk = candidate;

            while (walk.HasValue && seen.Add(walk.Value))
            {
                if (walk.Value == id) return true;
                walk = all.FirstOrDefault(m => m.Id == walk.Value)?.ParentId;
            }

            return false;
        }

        private static string SectionOf(Menu m, List<Menu> all) =>
            m.ParentId == null
                ? "Main"
                : all.FirstOrDefault(p => p.Id == m.ParentId)?.MenuTitle ?? "Main";

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static List<PageDto> ToDtos(List<Menu> menus) => ToDtos(menus, menus);

        private static List<PageDto> ToDtos(List<Menu> menus, List<Menu> all) =>
            menus.Select(m => ToDto(m, all)).ToList();

        private static PageDto ToDto(Menu m, List<Menu> all) => new()
        {
            Id             = m.Id,
            SrNo           = m.SrNo ?? m.Id,
            PageName       = m.MenuTitle,
            Url            = m.Url ?? string.Empty,
            RoutingPageUrl = m.RoutingPageUrl ?? m.Url ?? string.Empty,
            Icon           = m.Icon ?? string.Empty,
            // Not stored: the section a page belongs to IS its parent, so
            // reading it from the parent cannot drift out of step with the tree.
            MenuPage       = SectionOf(m, all),
            IsActive       = m.IsActive,
            ParentId       = m.ParentId,
            DisplayOrder   = m.DisplayOrder
        };

        private static List<PageTreeItemDto> BuildTree(List<Menu> all, int? parentId) =>
            all.Where(m => m.ParentId == parentId)
               .OrderBy(m => m.DisplayOrder)
               .Select(m => new PageTreeItemDto
               {
                   Id         = m.Id,
                   Name       = m.MenuTitle,
                   Icon       = m.Icon ?? string.Empty,
                   ParentId   = m.ParentId,
                   IsExpanded = true,
                   Children   = BuildTree(all, m.Id)
               })
               .ToList();
    }
}
