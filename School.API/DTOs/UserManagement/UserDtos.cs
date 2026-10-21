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

        /// <summary>
        /// The roles this user is to have, from the Users form's Roles picker.
        /// The form has always shown the picker; nothing was ever done with the
        /// selection, so every user was saved with no role at all.
        ///
        /// Null means "leave the roles alone" (an older client that does not
        /// send the field); an empty list means "this user has no roles".
        /// </summary>
        public List<int>? RoleIds { get; set; }
    }

    /// <summary>One user's role assignment, for the list and the edit form.</summary>
    public class UserRolesDto
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new();

        /// <summary>"Admin, Accountant" — what the list column prints.</summary>
        public string RoleNames { get; set; } = string.Empty;
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
