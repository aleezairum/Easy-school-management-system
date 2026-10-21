namespace School.API.DTOs
{
    /// <summary>
    /// The School Profile form, in the shape the form itself uses.
    /// </summary>
    /// <remarks>
    /// The form calls the school's name "instituteName" while the column is
    /// "Name"; the mapping is done in the controller so neither side has to be
    /// renamed. Everything is optional except the name, the phone and the
    /// address, which the form already insists on before it posts.
    /// </remarks>
    public class InstituteDto
    {
        public int Id { get; set; }

        public string InstituteName { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Tagline { get; set; }
        public string? RegistrationNo { get; set; }
        public int? EstablishedYear { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        public string? PrincipalName { get; set; }
        public string? PrincipalPhone { get; set; }
        public string? PrincipalEmail { get; set; }

        /// <summary>A data URL, or null when no logo has been chosen.</summary>
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
