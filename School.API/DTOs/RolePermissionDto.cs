namespace School.API.DTOs
{
    public class RolePermissionDto
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

    //public class MenuForPermissionDto
    //{
    //    public int Id { get; set; }
    //    public string MenuTitle { get; set; } = string.Empty;
    //    public int? ParentId { get; set; }
    //    public int Level { get; set; }
    //    public int DisplayOrder { get; set; }
    //}
}
