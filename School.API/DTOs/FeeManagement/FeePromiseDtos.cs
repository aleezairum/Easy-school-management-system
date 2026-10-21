namespace School.API.DTOs.FeeManagement
{
    /// <summary>
    /// A parent's promise to clear a student's dues by a date.
    /// </summary>
    /// <remarks>
    /// While the promise is in force the student drops off the unpaid-fee list
    /// and receives no fee reminders. The day after the promise date, if the
    /// dues are still there, they are back on the list — nothing runs, the
    /// lists simply compare the date. See Scripts/FeePromise.sql.
    /// </remarks>
    public class FeePromiseDto
    {
        public int VID { get; set; }
        public int StudentID { get; set; }

        public string? RollNo { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? FatherCNIC { get; set; }
        public string? ContactNo { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? CampusName { get; set; }

        public DateTime PromiseDate { get; set; }

        /// <summary>What they said they would pay. Null means the whole amount.</summary>
        public decimal? PromisedAmount { get; set; }

        /// <summary>Who gave their word — father, mother, guardian.</summary>
        public string? PromisedBy { get; set; }

        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// What the FAMILY still owes as at today. The promise covers every
        /// sibling, so this is the family's figure, not one child's share.
        /// </summary>
        public decimal PendingAmount { get; set; }

        /// <summary>How many children of this parent the promise speaks for.</summary>
        public int SiblingCount { get; set; } = 1;

        /// <summary>Days until the promise date. Negative once it has passed.</summary>
        public int DaysLeft { get; set; }

        /// <summary>1 = in force, 2 = lapsed and still owing, 3 = settled.</summary>
        public byte PromiseStatus { get; set; }
    }

    /// <summary>
    /// One line of the printed Promise to Pay report: one row per STUDENT the
    /// promise covers, not per promise.
    /// </summary>
    /// <remarks>
    /// A promise is taken against one child and holds for every sibling of the
    /// same parent, so a sheet listing only the child who was written down
    /// would send the office chasing the other three. See
    /// Scripts/SpRPT_SMSFeePromise.sql.
    /// </remarks>
    public class FeePromiseReportDto
    {
        public int PromiseID { get; set; }
        public DateTime PromiseDate { get; set; }
        public decimal? PromisedAmount { get; set; }
        public string? PromisedBy { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }

        /// <summary>The child the promise was actually written against.</summary>
        public string PromisedForName { get; set; } = string.Empty;

        public int StudentID { get; set; }
        public string? RollNo { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? ClassRollNo { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? CampusName { get; set; }
        public string? FatherName { get; set; }
        public string? ContactNo { get; set; }

        /// <summary>What THIS child owes as at today.</summary>
        public decimal OwnDues { get; set; }

        /// <summary>What the whole family owes as at today — what was promised.</summary>
        public decimal FamilyDues { get; set; }

        public int DaysLeft { get; set; }

        /// <summary>1 = in force, 2 = lapsed and still owing, 3 = settled.</summary>
        public byte PromiseStatus { get; set; }
    }

    /// <summary>What the form sends when a promise is taken or changed.</summary>
    public class SaveFeePromiseDto
    {
        public int VID { get; set; }
        public int StudentID { get; set; }
        public DateTime PromiseDate { get; set; }
        public decimal? PromisedAmount { get; set; }
        public string? PromisedBy { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
