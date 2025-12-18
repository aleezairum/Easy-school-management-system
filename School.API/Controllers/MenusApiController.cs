using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public MenusApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/MenusApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuForPermissionDto>>> GetMenus()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.ParentId ?? 0)
                .ThenBy(m => m.DisplayOrder)
                .Select(m => new MenuForPermissionDto
                {
                    Id = m.Id,
                    MenuTitle = m.MenuTitle,
                    ParentId = m.ParentId,
                    Level = m.Level,
                    DisplayOrder = m.DisplayOrder
                })
                .ToListAsync();

            return Ok(menus);
        }

        // GET: api/MenusApi/hierarchy
        [HttpGet("hierarchy")]
        public async Task<ActionResult<IEnumerable<MenuForPermissionDto>>> GetMenusHierarchy()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            // Build hierarchical list
            var result = new List<MenuForPermissionDto>();

            // Get root menus first
            var rootMenus = menus.Where(m => m.ParentId == null).OrderBy(m => m.DisplayOrder);

            foreach (var root in rootMenus)
            {
                result.Add(new MenuForPermissionDto
                {
                    Id = root.Id,
                    MenuTitle = root.MenuTitle,
                    ParentId = root.ParentId,
                    Level = root.Level,
                    DisplayOrder = root.DisplayOrder
                });

                // Add children
                AddChildren(menus, result, root.Id);
            }

            return Ok(result);
        }

        private void AddChildren(List<Models.Menu> allMenus, List<MenuForPermissionDto> result, int parentId)
        {
            var children = allMenus.Where(m => m.ParentId == parentId).OrderBy(m => m.DisplayOrder);

            foreach (var child in children)
            {
                result.Add(new MenuForPermissionDto
                {
                    Id = child.Id,
                    MenuTitle = child.MenuTitle,
                    ParentId = child.ParentId,
                    Level = child.Level,
                    DisplayOrder = child.DisplayOrder
                });

                // Recursively add children
                AddChildren(allMenus, result, child.Id);
            }
        }
    }
}
