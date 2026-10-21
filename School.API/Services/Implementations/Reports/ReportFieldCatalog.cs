// Services/Implementations/Reports/ReportFieldCatalog.cs
namespace School.API.Services.Implementations.Reports
{
    public enum ReportFieldType { Text, Number, Date, Bool }

    /// <summary>One selectable column, and the SQL behind it.</summary>
    public sealed class ReportField
    {
        /// <summary>What the screen sends back. Never interpolated as SQL.</summary>
        public string Key { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        /// <summary>Which fieldset the screen groups it under.</summary>
        public string Group { get; init; } = string.Empty;

        /// <summary>
        /// The SQL expression. This string comes from THIS FILE ONLY — it is
        /// never built from anything a caller sends.
        /// </summary>
        public string Sql { get; init; } = string.Empty;

        public ReportFieldType Type { get; init; } = ReportFieldType.Text;

        /// <summary>Ticked by default when the screen first opens.</summary>
        public bool Default { get; init; }

        /// <summary>
        /// Names a lookup endpoint so the filter box can offer a dropdown
        /// instead of free text (e.g. "class", "section"). Null = free text.
        /// </summary>
        public string? Lookup { get; init; }
    }

    /// <summary>
    /// The complete list of what a custom report may select and filter on.
    /// </summary>
    /// <remarks>
    /// This is the security boundary of the whole custom-report feature, and it
    /// is the reason the feature is safe to expose at all.
    ///
    /// A report builder is, by nature, the caller choosing columns and WHERE
    /// clauses — which is one careless string concatenation away from handing
    /// out the entire database. So nothing a caller sends ever becomes SQL.
    /// The caller sends KEYS; a key is looked up here and the SQL expression
    /// comes from this file. A key that is not in this table is rejected, not
    /// escaped, not sanitised — rejected. Values are always parameters.
    ///
    /// Adding a column to a report therefore means adding a row here, on
    /// purpose: it forces someone to decide that the column is fit to be
    /// exported before anyone can export it.
    /// </remarks>
    public static class ReportFieldCatalog
    {
        public const string Student = "student";
        public const string Employee = "employee";

        public static bool IsKnownEntity(string? entity) =>
            entity is Student or Employee;

        /// <summary>
        /// The FROM clause for an entity. Also from this file only.
        /// </summary>
        /// <remarks>
        /// The student query hangs off the LATEST academic record rather than
        /// joining SMSStudentAcademic directly: a child who has been at the
        /// school four years has four rows, and a plain join would list them
        /// four times, each with a different class. OUTER APPLY picks the
        /// current one, which is the row every other report in the system uses.
        /// </remarks>
        public static string From(string entity) => entity switch
        {
            Student => @"
                FROM SMSStudent S
                LEFT JOIN SMSParent P ON P.VID = S.ParentID
                OUTER APPLY (
                    SELECT TOP 1 A.SectionID, A.StatusID, A.GradeID, A.SessionID,
                           A.RollNo, A.Fee, A.IsAvailAcademy
                    FROM SMSStudentAcademic A
                    WHERE A.StudentID = S.VID AND ISNULL(A.IsActive, 1) = 1
                    ORDER BY A.SessionID DESC, A.VID DESC
                ) A
                LEFT JOIN SMSSection          SEC ON SEC.VID = A.SectionID
                LEFT JOIN SMSClass            C   ON C.VID   = SEC.ClassID
                LEFT JOIN SMSCampus           CP  ON CP.VID  = S.CampusID
                LEFT JOIN AcademicGrades      G   ON G.VID   = A.GradeID
                LEFT JOIN AcademicSessionYear SY  ON SY.VID  = A.SessionID
                LEFT JOIN SMSStudentStatus    ST  ON ST.VID  = A.StatusID",

            Employee => @"
                FROM Employees E
                LEFT JOIN Designations D  ON D.Id  = E.DesignationId
                LEFT JOIN HRGrades     HG ON HG.Id = E.HRGradeId
                LEFT JOIN HrStaffCategory SC ON SC.Id = E.StaffCategoryId
                LEFT JOIN SMSCampus    CP ON CP.VID = E.CampusID",

            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown report entity.")
        };

        /// <summary>The default ORDER BY when the caller does not choose one.</summary>
        public static string DefaultSort(string entity) => entity switch
        {
            Student => "C.VID, SEC.VName, S.Name",
            Employee => "E.FirstName, E.LastName",
            _ => "1"
        };

        public static IReadOnlyList<ReportField> Fields(string entity) => entity switch
        {
            Student => StudentFields,
            Employee => EmployeeFields,
            _ => Array.Empty<ReportField>()
        };

        public static ReportField? Find(string entity, string? key) =>
            string.IsNullOrWhiteSpace(key)
                ? null
                : Fields(entity).FirstOrDefault(
                    f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

        // ── Student ──────────────────────────────────────────────────────
        private static readonly ReportField[] StudentFields =
        {
            new() { Key = "rollNo",        Label = "Roll No",         Group = "Student",  Sql = "S.RollNo",        Type = ReportFieldType.Text,   Default = true },
            new() { Key = "name",          Label = "Student Name",    Group = "Student",  Sql = "S.Name",          Type = ReportFieldType.Text,   Default = true },
            new() { Key = "nameUrdu",      Label = "Name (Urdu)",     Group = "Student",  Sql = "S.Name_Urdu",     Type = ReportFieldType.Text },
            new() { Key = "admissionNo",   Label = "Admission No",    Group = "Student",  Sql = "S.AdmissionNo",   Type = ReportFieldType.Text },
            new() { Key = "registrationNo",Label = "Registration No", Group = "Student",  Sql = "S.RegistrationNo",Type = ReportFieldType.Text },
            new() { Key = "admissionDate", Label = "Admission Date",  Group = "Student",  Sql = "S.AdmissionDate", Type = ReportFieldType.Date },
            new() { Key = "gender",        Label = "Gender",          Group = "Student",  Sql = "S.Gender",        Type = ReportFieldType.Text },
            new() { Key = "dob",           Label = "Date of Birth",   Group = "Student",  Sql = "S.DOB",           Type = ReportFieldType.Date },
            new() { Key = "birthPlace",    Label = "Birth Place",     Group = "Student",  Sql = "S.BirthPlace",    Type = ReportFieldType.Text },
            new() { Key = "formBNo",       Label = "Form-B No",       Group = "Student",  Sql = "S.FormBNo",       Type = ReportFieldType.Text },
            new() { Key = "religion",      Label = "Religion",        Group = "Student",  Sql = "S.Religion",      Type = ReportFieldType.Text },
            new() { Key = "caste",         Label = "Caste",           Group = "Student",  Sql = "S.Caste",         Type = ReportFieldType.Text },
            new() { Key = "address",       Label = "Address",         Group = "Student",  Sql = "S.Address",       Type = ReportFieldType.Text },
            new() { Key = "studentMobile", Label = "Student Mobile",  Group = "Student",  Sql = "S.MobileNo",      Type = ReportFieldType.Text },

            new() { Key = "campus",  Label = "Campus",  Group = "Class",  Sql = "CP.VName",  Type = ReportFieldType.Text, Lookup = "campus" },
            new() { Key = "class",   Label = "Class",   Group = "Class",  Sql = "C.VName",   Type = ReportFieldType.Text, Default = true, Lookup = "class" },
            new() { Key = "section", Label = "Section", Group = "Class",  Sql = "SEC.VName", Type = ReportFieldType.Text, Default = true, Lookup = "section" },
            new() { Key = "session", Label = "Session", Group = "Class",  Sql = "SY.VName",  Type = ReportFieldType.Text },
            new() { Key = "grade",   Label = "Grade",   Group = "Class",  Sql = "G.VName",   Type = ReportFieldType.Text },
            new() { Key = "status",  Label = "Status",  Group = "Class",  Sql = "ST.VName",  Type = ReportFieldType.Text, Default = true, Lookup = "studentStatus" },
            new() { Key = "fee",     Label = "Fee",     Group = "Class",  Sql = "A.Fee",     Type = ReportFieldType.Number },
            new() { Key = "availAcademy", Label = "Avails Academy", Group = "Class", Sql = "CAST(ISNULL(A.IsAvailAcademy, 0) AS BIT)", Type = ReportFieldType.Bool },

            new() { Key = "fatherName",     Label = "Father Name",      Group = "Family", Sql = "P.FatherName",     Type = ReportFieldType.Text, Default = true },
            new() { Key = "fatherCnic",     Label = "Father CNIC",      Group = "Family", Sql = "P.FatherCNIC",     Type = ReportFieldType.Text },
            new() { Key = "fatherMobile",   Label = "Father Mobile",    Group = "Family", Sql = "P.FatherMobile",   Type = ReportFieldType.Text, Default = true },
            new() { Key = "occupation",     Label = "Father Occupation",Group = "Family", Sql = "P.Occupation",     Type = ReportFieldType.Text },
            new() { Key = "motherName",     Label = "Mother Name",      Group = "Family", Sql = "P.MotherName",     Type = ReportFieldType.Text },
            new() { Key = "motherMobile",   Label = "Mother Mobile",    Group = "Family", Sql = "P.MotherMobile",   Type = ReportFieldType.Text },
            new() { Key = "guardianName",   Label = "Guardian Name",    Group = "Family", Sql = "P.GuardianName",   Type = ReportFieldType.Text },
            new() { Key = "guardianMobile", Label = "Guardian Mobile",  Group = "Family", Sql = "P.GuardianMobile", Type = ReportFieldType.Text },
            new() { Key = "guardianRelation", Label = "Guardian Relation", Group = "Family", Sql = "P.GuardianRelation", Type = ReportFieldType.Text }
        };

        // ── Employee ─────────────────────────────────────────────────────
        private static readonly ReportField[] EmployeeFields =
        {
            new() { Key = "employeeCode", Label = "Employee Code", Group = "Employee", Sql = "E.EmployeeCode", Type = ReportFieldType.Text, Default = true },
            // Built here rather than read from the VName computed column so a
            // missing last name does not leave a trailing space in the export.
            new() { Key = "name",         Label = "Name",          Group = "Employee", Sql = "LTRIM(RTRIM(E.FirstName + ISNULL(' ' + E.LastName, '')))", Type = ReportFieldType.Text, Default = true },
            new() { Key = "fatherName",   Label = "Father Name",   Group = "Employee", Sql = "E.FatherName",   Type = ReportFieldType.Text, Default = true },
            new() { Key = "campus",       Label = "Campus",        Group = "Employee", Sql = "CP.VName",       Type = ReportFieldType.Text, Default = true, Lookup = "campus" },
            new() { Key = "designation",  Label = "Designation",   Group = "Employee", Sql = "D.Name",         Type = ReportFieldType.Text, Default = true, Lookup = "designation" },
            new() { Key = "staffCategory",Label = "Category",      Group = "Employee", Sql = "SC.Name",        Type = ReportFieldType.Text, Lookup = "staffCategory" },
            new() { Key = "department",   Label = "Department",    Group = "Employee", Sql = "E.Department",   Type = ReportFieldType.Text },
            new() { Key = "hrGrade",      Label = "HR Grade",      Group = "Employee", Sql = "HG.Name",        Type = ReportFieldType.Text },
            new() { Key = "employeeType", Label = "Employee Type", Group = "Employee", Sql = "E.EmployeeType", Type = ReportFieldType.Text },
            new() { Key = "status",       Label = "Status",        Group = "Employee", Sql = "E.Status",       Type = ReportFieldType.Text },
            new() { Key = "isActive",     Label = "Active",        Group = "Employee", Sql = "E.IsActive",     Type = ReportFieldType.Bool },
            new() { Key = "isTeaching",   Label = "Teaching Staff",Group = "Employee", Sql = "E.IsTeachingStaff", Type = ReportFieldType.Bool },

            new() { Key = "cnic",         Label = "CNIC",          Group = "Personal", Sql = "E.CNIC",         Type = ReportFieldType.Text },
            new() { Key = "gender",       Label = "Gender",        Group = "Personal", Sql = "E.Gender",       Type = ReportFieldType.Text },
            new() { Key = "dob",          Label = "Date of Birth", Group = "Personal", Sql = "E.DateOfBirth",  Type = ReportFieldType.Date, Default = true },
            new() { Key = "mobile",       Label = "Mobile",        Group = "Personal", Sql = "E.Mobile",       Type = ReportFieldType.Text, Default = true },
            new() { Key = "phone",        Label = "Phone",         Group = "Personal", Sql = "E.Phone",        Type = ReportFieldType.Text },
            new() { Key = "email",        Label = "Email",         Group = "Personal", Sql = "E.Email",        Type = ReportFieldType.Text },
            new() { Key = "address",      Label = "Address",       Group = "Personal", Sql = "E.Address",      Type = ReportFieldType.Text },
            new() { Key = "city",         Label = "City",          Group = "Personal", Sql = "E.City",         Type = ReportFieldType.Text },

            new() { Key = "qualification", Label = "Qualification", Group = "Service", Sql = "E.Qualification", Type = ReportFieldType.Text, Default = true },
            new() { Key = "specialization",Label = "Specialization",Group = "Service", Sql = "E.Specialization",Type = ReportFieldType.Text },
            new() { Key = "experience",    Label = "Experience",    Group = "Service", Sql = "E.Experience",    Type = ReportFieldType.Text },
            new() { Key = "joiningDate",   Label = "Joining Date",  Group = "Service", Sql = "E.JoiningDate",   Type = ReportFieldType.Date, Default = true },
            new() { Key = "leavingDate",   Label = "Leaving Date",  Group = "Service", Sql = "E.LeavingDate",   Type = ReportFieldType.Date },
            new() { Key = "machineCardNo", Label = "Machine Card",  Group = "Service", Sql = "E.MachineCardNo", Type = ReportFieldType.Text },

            new() { Key = "bankName",      Label = "Bank",            Group = "Bank", Sql = "E.BankName",      Type = ReportFieldType.Text },
            new() { Key = "bankAccountNo", Label = "Account No",      Group = "Bank", Sql = "E.BankAccountNo", Type = ReportFieldType.Text },

            new() { Key = "emergencyName",     Label = "Emergency Contact",  Group = "Emergency", Sql = "E.EmergencyContactName",     Type = ReportFieldType.Text },
            new() { Key = "emergencyPhone",    Label = "Emergency Phone",    Group = "Emergency", Sql = "E.EmergencyContactPhone",    Type = ReportFieldType.Text },
            new() { Key = "emergencyRelation", Label = "Emergency Relation", Group = "Emergency", Sql = "E.EmergencyContactRelation", Type = ReportFieldType.Text }
        };

        // ── Operators ────────────────────────────────────────────────────
        //
        // Same rule as the fields: the caller sends a name, the SQL comes from
        // here. ValueCount says how many parameters the operator consumes, so
        // the builder never has to guess.
        public sealed record ReportOperator(
            string Key, string Label, string Template, int ValueCount, ReportFieldType[] AppliesTo);

        private static readonly ReportFieldType[] AnyType =
            { ReportFieldType.Text, ReportFieldType.Number, ReportFieldType.Date, ReportFieldType.Bool };

        private static readonly ReportFieldType[] Ordered =
            { ReportFieldType.Number, ReportFieldType.Date };

        private static readonly ReportFieldType[] TextOnly =
            { ReportFieldType.Text };

        public static readonly ReportOperator[] Operators =
        {
            new("eq",       "is",           "{0} = {1}",                       1, AnyType),
            new("ne",       "is not",       "{0} <> {1}",                      1, AnyType),
            new("contains", "contains",     "{0} LIKE '%' + {1} + '%'",        1, TextOnly),
            new("starts",   "starts with",  "{0} LIKE {1} + '%'",              1, TextOnly),
            new("ends",     "ends with",    "{0} LIKE '%' + {1}",              1, TextOnly),
            new("gt",       "greater than", "{0} > {1}",                       1, Ordered),
            new("gte",      "on or after",  "{0} >= {1}",                      1, Ordered),
            new("lt",       "less than",    "{0} < {1}",                       1, Ordered),
            new("lte",      "on or before", "{0} <= {1}",                      1, Ordered),
            new("between",  "between",      "{0} BETWEEN {1} AND {2}",         2, Ordered),
            new("isnull",   "is blank",     "({0} IS NULL OR LTRIM(RTRIM(CAST({0} AS NVARCHAR(MAX)))) = '')", 0, AnyType),
            new("notnull",  "is not blank", "({0} IS NOT NULL AND LTRIM(RTRIM(CAST({0} AS NVARCHAR(MAX)))) <> '')", 0, AnyType)
        };

        public static ReportOperator? FindOperator(string? key) =>
            string.IsNullOrWhiteSpace(key)
                ? null
                : Operators.FirstOrDefault(
                    o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}
