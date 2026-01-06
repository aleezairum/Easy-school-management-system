namespace School.API.DTOs
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Badge { get; set; }
        public int Order { get; set; }
        public string Section { get; set; } = string.Empty;
    }

    public class MenuSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public List<MenuItemDto> Items { get; set; } = new();
    }

    public class SidebarDataDto
    {
        public string LogoUrl { get; set; } = string.Empty;
        public string InstituteName { get; set; } = string.Empty;
        public string InstituteTagline { get; set; } = string.Empty;
        public List<MenuSectionDto> MenuSections { get; set; } = new();
    }

    public class MenuDto
    {
        public int Id { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
        public string? ParentTitle { get; set; }
        public int DisplayOrder { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateMenuDto
    {
        public string MenuTitle { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateMenuDto
    {
        public int Id { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class MenuForPermissionDto
    {
        public int Id { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
    }
}
