namespace School.API.DTOs.UserManagement
{
    public class UserSaveDto
    {
        public int VID { get; set; } = 0;
        public int? EmployeeID { get; set; }
        public string? UserFullName { get; set; }
        public string? UserLogin { get; set; }
        public string? UserPassword { get; set; }
        public bool AllowAudit { get; set; } = false;
        public bool AllowActual { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public int UserType { get; set; } = 0;
        public bool IsIndividual { get; set; } = false;
        public int DefaultCompanyID { get; set; } = 0;
        public int DefaultFinancialYearID { get; set; } = 0;
        public int UserSecFlage { get; set; } = 0;
        public string? MacAddress { get; set; }
        public string? ComputerName { get; set; }
        public string? WindowLoginName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserLoginDto
    {
        public string? UserLogin { get; set; }
        public string? UserPassword { get; set; }
    }

    public class UserLoginResponseDto
    {
        public int VID { get; set; }
        public string? UserFullName { get; set; }
        public string? UserLogin { get; set; }
        public bool IsAdmin { get; set; }
        public int UserType { get; set; }
    }
}
