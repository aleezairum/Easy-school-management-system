namespace School.Web.Models
{
    public class RoleViewModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleListViewModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RolePermissionViewModel
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int MenuId { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public bool IsView { get; set; }
        public bool IsInsert { get; set; }
        public bool IsUpdate { get; set; }
        public bool IsDelete { get; set; }
        public bool IsActive { get; set; }
        public bool IsBackDate { get; set; }
        public bool IsPrint { get; set; }
    }

    public class RoleWithPermissionsViewModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<RolePermissionViewModel> Permissions { get; set; } = new List<RolePermissionViewModel>();
    }

    public class MenuForPermissionViewModel
    {
        public int Id { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class RolesIndexViewModel
    {
        public List<RoleListViewModel> Roles { get; set; } = new List<RoleListViewModel>();
        public List<MenuForPermissionViewModel> Menus { get; set; } = new List<MenuForPermissionViewModel>();
    }
}
