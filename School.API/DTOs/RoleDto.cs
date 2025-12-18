namespace School.API.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleListDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RoleWithPermissionsDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<RolePermissionDto> Permissions { get; set; } = new List<RolePermissionDto>();
    }

    public class SaveRoleRequestDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<RolePermissionDto> Permissions { get; set; } = new List<RolePermissionDto>();
    }
}
