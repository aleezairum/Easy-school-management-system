namespace School.Web.Models
{
    public class UserViewModel
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

    public class UserListViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string Password { get; set; } = "********";
        public string UserRoles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class EmployeeDropdownViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
    }

    public class RoleDropdownViewModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class UsersIndexViewModel
    {
        public List<UserListViewModel> Users { get; set; } = new List<UserListViewModel>();
        public List<EmployeeDropdownViewModel> Employees { get; set; } = new List<EmployeeDropdownViewModel>();
        public List<RoleDropdownViewModel> Roles { get; set; } = new List<RoleDropdownViewModel>();
    }
}
