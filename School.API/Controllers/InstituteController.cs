using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    /// <summary>
    /// The School Profile form (/my-institute).
    /// </summary>
    /// <remarks>
    /// One school, one row. The form does not ask which institute it is
    /// editing and has no list, so this is a GET-and-save pair rather than the
    /// usual CRUD: GET returns the profile (creating an empty one the first
    /// time so the form never has to handle a 404), and POST writes it back.
    ///
    /// Every endpoint here was commented out, so the form could neither load
    /// nor save anything.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class InstituteController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public InstituteController(SchoolDbContext context)
        {
            _context = context;
        }

        /// <summary>The school profile. Never 404s — the form expects an object.</summary>
        [HttpGet]
        public async Task<ActionResult<InstituteDto>> GetProfile()
        {
            var institute = await CurrentAsync();

            if (institute == null)
            {
                // First run on a fresh database: hand the form an empty profile
                // rather than an error, so it opens blank and saves normally.
                return Ok(new InstituteDto());
            }

            return Ok(ToDto(institute));
        }

        /// <summary>Saves the profile, creating it on first save.</summary>
        [HttpPost]
        public async Task<ActionResult<InstituteDto>> SaveProfile([FromBody] InstituteDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.InstituteName))
                return BadRequest(new { message = "Institute name is required." });

            var institute = await CurrentAsync();
            var isNew = institute == null;

            if (isNew)
            {
                institute = new Institute { CreatedAt = DateTime.Now };
                _context.Set<Institute>().Add(institute);
            }

            institute!.Name = dto.InstituteName.Trim();

            // Code is NOT NULL on the table but the form never asks for one, so
            // derive it from the name rather than refusing the save.
            institute.Code = string.IsNullOrWhiteSpace(dto.Code)
                ? (string.IsNullOrWhiteSpace(institute.Code) ? DeriveCode(institute.Name) : institute.Code)
                : dto.Code.Trim();

            institute.Tagline         = Clean(dto.Tagline);
            institute.RegistrationNo  = Clean(dto.RegistrationNo);
            institute.EstablishedYear = dto.EstablishedYear;
            institute.Phone           = Clean(dto.Phone);
            institute.Email           = Clean(dto.Email);
            institute.Website         = Clean(dto.Website);
            institute.Address         = Clean(dto.Address);
            institute.City            = Clean(dto.City);
            institute.State           = Clean(dto.State);
            institute.Country         = Clean(dto.Country);
            institute.PostalCode      = Clean(dto.PostalCode);
            institute.PrincipalName   = Clean(dto.PrincipalName);
            institute.PrincipalPhone  = Clean(dto.PrincipalPhone);
            institute.PrincipalEmail  = Clean(dto.PrincipalEmail);

            // A logo is only replaced when one is sent. Saving the form after
            // editing a phone number must not wipe the logo that is already
            // there, and the form only sends the picture when it is changed.
            if (!string.IsNullOrWhiteSpace(dto.LogoUrl))
                institute.LogoUrl = dto.LogoUrl;

            institute.IsActive  = true;
            institute.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(ToDto(institute));
        }

        /// <summary>Removes the logo, leaving the rest of the profile alone.</summary>
        [HttpDelete("logo")]
        public async Task<IActionResult> DeleteLogo()
        {
            var institute = await CurrentAsync();
            if (institute == null) return Ok(new { message = "Nothing to remove." });

            institute.LogoUrl = null;
            institute.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Logo removed." });
        }

        // The profile row: the active one, else whatever is there. Ordered by
        // Id so a database that somehow holds two rows always answers with the
        // same one rather than alternating between them.
        private Task<Institute?> CurrentAsync() =>
            _context.Set<Institute>()
                    .OrderByDescending(i => i.IsActive)
                    .ThenBy(i => i.Id)
                    .FirstOrDefaultAsync();

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // "SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)" -> "SSMHSBG"
        private static string DeriveCode(string name)
        {
            var letters = name.Split(new[] { ' ', '-', '(', ')', '&', ',', '.' },
                                     StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => char.IsLetter(w[0]))
                              .Select(w => char.ToUpperInvariant(w[0]));

            var code = new string(letters.ToArray());
            return string.IsNullOrEmpty(code) ? "SCHOOL" : code[..Math.Min(code.Length, 20)];
        }

        private static InstituteDto ToDto(Institute i) => new()
        {
            Id              = i.Id,
            InstituteName   = i.Name,
            Code            = i.Code,
            Tagline         = i.Tagline,
            RegistrationNo  = i.RegistrationNo,
            EstablishedYear = i.EstablishedYear,
            Phone           = i.Phone,
            Email           = i.Email,
            Website         = i.Website,
            Address         = i.Address,
            City            = i.City,
            State           = i.State,
            Country         = i.Country,
            PostalCode      = i.PostalCode,
            PrincipalName   = i.PrincipalName,
            PrincipalPhone  = i.PrincipalPhone,
            PrincipalEmail  = i.PrincipalEmail,
            LogoUrl         = i.LogoUrl,
            IsActive        = i.IsActive
        };
    }
}
