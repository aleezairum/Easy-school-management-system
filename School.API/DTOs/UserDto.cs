namespace School.API.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool IsActive { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
        public string? RoleNames { get; set; }
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string Password { get; set; } = "********";
        public string UserRoles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateUserDto
    {
        public int? EmployeeId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    public class UpdateUserDto
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string? Password { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
    }
}
