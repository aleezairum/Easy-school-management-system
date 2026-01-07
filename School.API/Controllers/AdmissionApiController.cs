using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdmissionApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public AdmissionApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/AdmissionApi
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<AdmissionListDto>>> GetAdmissions()
        //{
        //    var admissions = await _context.Admissions
        //        .OrderByDescending(a => a.CreatedAt)
        //        .Select(a => new AdmissionListDto
        //        {
        //            Id = a.Id,
        //            AdmissionNo = a.AdmissionNo,
        //            NameOfStudent = a.NameOfStudent,
        //            FatherName = a.FatherName,
        //            ClassSought = a.ClassSought,
        //            DateOfAdmission = a.DateOfAdmission,
        //            FatherMobile = a.FatherMobile,
        //            Status = a.Status,
        //            IsActive = a.IsActive
        //        })
        //        .ToListAsync();

        //    return Ok(admissions);
        //}

        //// GET: api/AdmissionApi/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<AdmissionDto>> GetAdmission(int id)
        //{
        //    var admission = await _context.Admissions
        //        .Where(a => a.Id == id)
        //        .Select(a => new AdmissionDto
        //        {
        //            Id = a.Id,
        //            DateOfAdmission = a.DateOfAdmission,
        //            AdmissionNo = a.AdmissionNo,
        //            ClassSought = a.ClassSought,
        //            NameOfStudent = a.NameOfStudent,
        //            NameOfStudentUrdu = a.NameOfStudentUrdu,
        //            FatherName = a.FatherName,
        //            FatherNameUrdu = a.FatherNameUrdu,
        //            FatherCNIC = a.FatherCNIC,
        //            FatherOccupation = a.FatherOccupation,
        //            FatherMobile = a.FatherMobile,
        //            MotherName = a.MotherName,
        //            MotherCNIC = a.MotherCNIC,
        //            MotherMobile = a.MotherMobile,
        //            GuardianName = a.GuardianName,
        //            GuardianCNIC = a.GuardianCNIC,
        //            GuardianRelation = a.GuardianRelation,
        //            GuardianMobile = a.GuardianMobile,
        //            DateOfBirth = a.DateOfBirth,
        //            DateOfBirthInWords = a.DateOfBirthInWords,
        //            PlaceOfBirth = a.PlaceOfBirth,
        //            FormBNo = a.FormBNo,
        //            Gender = a.Gender,
        //            Religion = a.Religion,
        //            PresentAddress = a.PresentAddress,
        //            PresentAddressUrdu = a.PresentAddressUrdu,
        //            PermanentAddress = a.PermanentAddress,
        //            PermanentAddressUrdu = a.PermanentAddressUrdu,
        //            PhoneResidence = a.PhoneResidence,
        //            EmergencyContact = a.EmergencyContact,
        //            PreviousSchool = a.PreviousSchool,
        //            LastClass = a.LastClass,
        //            Board = a.Board,
        //            YearOfPassing = a.YearOfPassing,
        //            MarksObtained = a.MarksObtained,
        //            TotalMarks = a.TotalMarks,
        //            Percentage = a.Percentage,
        //            RegistrationNo = a.RegistrationNo,
        //            RollNo = a.RollNo,
        //            AdmissionFee = a.AdmissionFee,
        //            TuitionFee = a.TuitionFee,
        //            OtherCharges = a.OtherCharges,
        //            TotalFee = a.TotalFee,
        //            TestMarks = a.TestMarks,
        //            TestTotalMarks = a.TestTotalMarks,
        //            TestPercentage = a.TestPercentage,
        //            TestGrade = a.TestGrade,
        //            Remarks = a.Remarks,
        //            Status = a.Status,
        //            IsActive = a.IsActive
        //        })
        //        .FirstOrDefaultAsync();

        //    if (admission == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(admission);
        //}

        //// GET: api/AdmissionApi/next-admission-no
        //[HttpGet("next-admission-no")]
        //public async Task<ActionResult<string>> GetNextAdmissionNo()
        //{
        //    var year = DateTime.Now.Year;
        //    var lastAdmission = await _context.Admissions
        //        .Where(a => a.AdmissionNo != null && a.AdmissionNo.StartsWith($"ADM-{year}"))
        //        .OrderByDescending(a => a.AdmissionNo)
        //        .FirstOrDefaultAsync();

        //    int nextNumber = 1;
        //    if (lastAdmission != null && !string.IsNullOrEmpty(lastAdmission.AdmissionNo))
        //    {
        //        var parts = lastAdmission.AdmissionNo.Split('-');
        //        if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
        //        {
        //            nextNumber = lastNumber + 1;
        //        }
        //    }

        //    return Ok($"ADM-{year}-{nextNumber:D4}");
        //}

        //// POST: api/AdmissionApi
        //[HttpPost]
        //public async Task<ActionResult<AdmissionDto>> CreateAdmission(CreateAdmissionDto request)
        //{
        //    // Generate admission number if not provided
        //    string admissionNo = request.AdmissionNo ?? "";
        //    if (string.IsNullOrEmpty(admissionNo))
        //    {
        //        var year = DateTime.Now.Year;
        //        var lastAdmission = await _context.Admissions
        //            .Where(a => a.AdmissionNo != null && a.AdmissionNo.StartsWith($"ADM-{year}"))
        //            .OrderByDescending(a => a.AdmissionNo)
        //            .FirstOrDefaultAsync();

        //        int nextNumber = 1;
        //        if (lastAdmission != null && !string.IsNullOrEmpty(lastAdmission.AdmissionNo))
        //        {
        //            var parts = lastAdmission.AdmissionNo.Split('-');
        //            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
        //            {
        //                nextNumber = lastNumber + 1;
        //            }
        //        }
        //        admissionNo = $"ADM-{year}-{nextNumber:D4}";
        //    }

        //    var admission = new Admission
        //    {
        //        DateOfAdmission = request.DateOfAdmission ?? DateTime.Now,
        //        AdmissionNo = admissionNo,
        //        ClassSought = request.ClassSought,
        //        NameOfStudent = request.NameOfStudent,
        //        NameOfStudentUrdu = request.NameOfStudentUrdu,
        //        FatherName = request.FatherName,
        //        FatherNameUrdu = request.FatherNameUrdu,
        //        FatherCNIC = request.FatherCNIC,
        //        FatherOccupation = request.FatherOccupation,
        //        FatherMobile = request.FatherMobile,
        //        MotherName = request.MotherName,
        //        MotherCNIC = request.MotherCNIC,
        //        MotherMobile = request.MotherMobile,
        //        GuardianName = request.GuardianName,
        //        GuardianCNIC = request.GuardianCNIC,
        //        GuardianRelation = request.GuardianRelation,
        //        GuardianMobile = request.GuardianMobile,
        //        DateOfBirth = request.DateOfBirth,
        //        DateOfBirthInWords = request.DateOfBirthInWords,
        //        PlaceOfBirth = request.PlaceOfBirth,
        //        FormBNo = request.FormBNo,
        //        Gender = request.Gender,
        //        Religion = request.Religion,
        //        PresentAddress = request.PresentAddress,
        //        PresentAddressUrdu = request.PresentAddressUrdu,
        //        PermanentAddress = request.PermanentAddress,
        //        PermanentAddressUrdu = request.PermanentAddressUrdu,
        //        PhoneResidence = request.PhoneResidence,
        //        EmergencyContact = request.EmergencyContact,
        //        PreviousSchool = request.PreviousSchool,
        //        LastClass = request.LastClass,
        //        Board = request.Board,
        //        YearOfPassing = request.YearOfPassing,
        //        MarksObtained = request.MarksObtained,
        //        TotalMarks = request.TotalMarks,
        //        Percentage = request.Percentage,
        //        RegistrationNo = request.RegistrationNo,
        //        RollNo = request.RollNo,
        //        AdmissionFee = request.AdmissionFee,
        //        TuitionFee = request.TuitionFee,
        //        OtherCharges = request.OtherCharges,
        //        TotalFee = request.TotalFee,
        //        TestMarks = request.TestMarks,
        //        TestTotalMarks = request.TestTotalMarks,
        //        TestPercentage = request.TestPercentage,
        //        TestGrade = request.TestGrade,
        //        Remarks = request.Remarks,
        //        Status = "Pending",
        //        IsActive = true,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _context.Admissions.Add(admission);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetAdmission), new { id = admission.Id }, new AdmissionDto
        //    {
        //        Id = admission.Id,
        //        AdmissionNo = admission.AdmissionNo,
        //        NameOfStudent = admission.NameOfStudent,
        //        FatherName = admission.FatherName,
        //        ClassSought = admission.ClassSought,
        //        DateOfAdmission = admission.DateOfAdmission,
        //        Status = admission.Status,
        //        IsActive = admission.IsActive
        //    });
        //}

        //// PUT: api/AdmissionApi/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateAdmission(int id, UpdateAdmissionDto request)
        //{
        //    if (id != request.Id)
        //    {
        //        return BadRequest(new { message = "ID mismatch" });
        //    }

        //    var admission = await _context.Admissions.FindAsync(id);

        //    if (admission == null)
        //    {
        //        return NotFound();
        //    }

        //    admission.DateOfAdmission = request.DateOfAdmission;
        //    admission.AdmissionNo = request.AdmissionNo;
        //    admission.ClassSought = request.ClassSought;
        //    admission.NameOfStudent = request.NameOfStudent;
        //    admission.NameOfStudentUrdu = request.NameOfStudentUrdu;
        //    admission.FatherName = request.FatherName;
        //    admission.FatherNameUrdu = request.FatherNameUrdu;
        //    admission.FatherCNIC = request.FatherCNIC;
        //    admission.FatherOccupation = request.FatherOccupation;
        //    admission.FatherMobile = request.FatherMobile;
        //    admission.MotherName = request.MotherName;
        //    admission.MotherCNIC = request.MotherCNIC;
        //    admission.MotherMobile = request.MotherMobile;
        //    admission.GuardianName = request.GuardianName;
        //    admission.GuardianCNIC = request.GuardianCNIC;
        //    admission.GuardianRelation = request.GuardianRelation;
        //    admission.GuardianMobile = request.GuardianMobile;
        //    admission.DateOfBirth = request.DateOfBirth;
        //    admission.DateOfBirthInWords = request.DateOfBirthInWords;
        //    admission.PlaceOfBirth = request.PlaceOfBirth;
        //    admission.FormBNo = request.FormBNo;
        //    admission.Gender = request.Gender;
        //    admission.Religion = request.Religion;
        //    admission.PresentAddress = request.PresentAddress;
        //    admission.PresentAddressUrdu = request.PresentAddressUrdu;
        //    admission.PermanentAddress = request.PermanentAddress;
        //    admission.PermanentAddressUrdu = request.PermanentAddressUrdu;
        //    admission.PhoneResidence = request.PhoneResidence;
        //    admission.EmergencyContact = request.EmergencyContact;
        //    admission.PreviousSchool = request.PreviousSchool;
        //    admission.LastClass = request.LastClass;
        //    admission.Board = request.Board;
        //    admission.YearOfPassing = request.YearOfPassing;
        //    admission.MarksObtained = request.MarksObtained;
        //    admission.TotalMarks = request.TotalMarks;
        //    admission.Percentage = request.Percentage;
        //    admission.RegistrationNo = request.RegistrationNo;
        //    admission.RollNo = request.RollNo;
        //    admission.AdmissionFee = request.AdmissionFee;
        //    admission.TuitionFee = request.TuitionFee;
        //    admission.OtherCharges = request.OtherCharges;
        //    admission.TotalFee = request.TotalFee;
        //    admission.TestMarks = request.TestMarks;
        //    admission.TestTotalMarks = request.TestTotalMarks;
        //    admission.TestPercentage = request.TestPercentage;
        //    admission.TestGrade = request.TestGrade;
        //    admission.Remarks = request.Remarks;
        //    admission.Status = request.Status;
        //    admission.IsActive = request.IsActive;
        //    admission.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Admission updated successfully" });
        //}

        //// PUT: api/AdmissionApi/5/status
        //[HttpPut("{id}/status")]
        //public async Task<IActionResult> UpdateAdmissionStatus(int id, UpdateAdmissionStatusDto request)
        //{
        //    if (id != request.Id)
        //    {
        //        return BadRequest(new { message = "ID mismatch" });
        //    }

        //    var admission = await _context.Admissions.FindAsync(id);

        //    if (admission == null)
        //    {
        //        return NotFound();
        //    }

        //    if (request.Status != "Pending" && request.Status != "Approved" && request.Status != "Rejected")
        //    {
        //        return BadRequest(new { message = "Invalid status. Must be Pending, Approved, or Rejected" });
        //    }

        //    admission.Status = request.Status;
        //    admission.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = $"Admission status updated to {request.Status}" });
        //}

        //// DELETE: api/AdmissionApi/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteAdmission(int id)
        //{
        //    var admission = await _context.Admissions.FindAsync(id);

        //    if (admission == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Admissions.Remove(admission);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Admission deleted successfully" });
        //}

        //// GET: api/AdmissionApi/search
        //[HttpGet("search")]
        //public async Task<ActionResult<IEnumerable<AdmissionListDto>>> SearchAdmissions(
        //    [FromQuery] string? name,
        //    [FromQuery] string? admissionNo,
        //    [FromQuery] string? status,
        //    [FromQuery] string? classSought)
        //{
        //    var query = _context.Admissions.AsQueryable();

        //    if (!string.IsNullOrEmpty(name))
        //    {
        //        query = query.Where(a => a.NameOfStudent.Contains(name) ||
        //                                 (a.FatherName != null && a.FatherName.Contains(name)));
        //    }

        //    if (!string.IsNullOrEmpty(admissionNo))
        //    {
        //        query = query.Where(a => a.AdmissionNo != null && a.AdmissionNo.Contains(admissionNo));
        //    }

        //    if (!string.IsNullOrEmpty(status))
        //    {
        //        query = query.Where(a => a.Status == status);
        //    }

        //    if (!string.IsNullOrEmpty(classSought))
        //    {
        //        query = query.Where(a => a.ClassSought != null && a.ClassSought.Contains(classSought));
        //    }

        //    var admissions = await query
        //        .OrderByDescending(a => a.CreatedAt)
        //        .Select(a => new AdmissionListDto
        //        {
        //            Id = a.Id,
        //            AdmissionNo = a.AdmissionNo,
        //            NameOfStudent = a.NameOfStudent,
        //            FatherName = a.FatherName,
        //            ClassSought = a.ClassSought,
        //            DateOfAdmission = a.DateOfAdmission,
        //            FatherMobile = a.FatherMobile,
        //            Status = a.Status,
        //            IsActive = a.IsActive
        //        })
        //        .ToListAsync();

        //    return Ok(admissions);
        //}
    }
}
