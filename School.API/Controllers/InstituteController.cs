using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstituteController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetInstitutes()
        {
            // TEMPORARY MOCK DATA (replace when DB is ready)
            var institutes = new List<InstituteDto>
            {
                new InstituteDto
                {
                    Id = 1,
                    Name = "Springfield High School",
                    Code = "SHS001",
                    Address = "123 Main Street, Springfield"
                },
                new InstituteDto
                {
                    Id = 2,
                    Name = "Riverside Academy",
                    Code = "RSA002",
                    Address = "456 River Road, Riverside"
                },
                new InstituteDto
                {
                    Id = 3,
                    Name = "Central Public School",
                    Code = "CPS003",
                    Address = "789 Central Avenue, Downtown"
                }
            };

            return Ok(institutes);
        }

        [HttpGet("{id}")]
        public IActionResult GetInstitute(int id)
        {
            // TEMPORARY MOCK DATA (replace when DB is ready)
            var institutes = new List<InstituteDto>
            {
                new InstituteDto
                {
                    Id = 1,
                    Name = "Springfield High School",
                    Code = "SHS001",
                    Address = "123 Main Street, Springfield"
                },
                new InstituteDto
                {
                    Id = 2,
                    Name = "Riverside Academy",
                    Code = "RSA002",
                    Address = "456 River Road, Riverside"
                },
                new InstituteDto
                {
                    Id = 3,
                    Name = "Central Public School",
                    Code = "CPS003",
                    Address = "789 Central Avenue, Downtown"
                }
            };

            var institute = institutes.FirstOrDefault(i => i.Id == id);

            if (institute == null)
            {
                return NotFound(new { message = "Institute not found" });
            }

            return Ok(institute);
        }
    }
}
