using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;
using School.API.Repositories.Interfaces.Student;

namespace School.API.Repositories.Implementations.Student
{
    public class StudentRepository : IStudentRepository
    {
        private readonly SchoolDbContext _context;

        public StudentRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentListDto>> GetAllAsync()
        {
            return await _context.Students
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new StudentListDto
                {
                    Id = s.Id,
                    RegistrationNo = s.RegistrationNo,
                    RollNo = s.RollNo,
                    NameOfStudent = s.NameOfStudent,
                    FatherName = s.FatherName,
                    CurrentClass = s.CurrentClass,
                    Section = s.Section,
                    FatherMobile = s.FatherMobile,
                    StudentPhoto = s.StudentPhoto,
                    Status = s.Status,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            return await _context.Students
                .Where(s => s.Id == id)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    AdmissionId = s.AdmissionId,
                    RegistrationNo = s.RegistrationNo,
                    RollNo = s.RollNo,
                    AdmissionNo = s.AdmissionNo,
                    DateOfAdmission = s.DateOfAdmission,
                    CurrentClass = s.CurrentClass,
                    Section = s.Section,
                    NameOfStudent = s.NameOfStudent,
                    NameOfStudentUrdu = s.NameOfStudentUrdu,
                    FatherName = s.FatherName,
                    FatherNameUrdu = s.FatherNameUrdu,
                    FatherCNIC = s.FatherCNIC,
                    FatherOccupation = s.FatherOccupation,
                    FatherMobile = s.FatherMobile,
                    MotherName = s.MotherName,
                    MotherCNIC = s.MotherCNIC,
                    MotherMobile = s.MotherMobile,
                    GuardianName = s.GuardianName,
                    GuardianCNIC = s.GuardianCNIC,
                    GuardianRelation = s.GuardianRelation,
                    GuardianMobile = s.GuardianMobile,
                    DateOfBirth = s.DateOfBirth,
                    DateOfBirthInWords = s.DateOfBirthInWords,
                    PlaceOfBirth = s.PlaceOfBirth,
                    FormBNo = s.FormBNo,
                    Gender = s.Gender,
                    Religion = s.Religion,
                    BloodGroup = s.BloodGroup,
                    Nationality = s.Nationality,
                    StudentPhoto = s.StudentPhoto,
                    PresentAddress = s.PresentAddress,
                    PresentAddressUrdu = s.PresentAddressUrdu,
                    PermanentAddress = s.PermanentAddress,
                    PermanentAddressUrdu = s.PermanentAddressUrdu,
                    PhoneResidence = s.PhoneResidence,
                    EmergencyContact = s.EmergencyContact,
                    Email = s.Email,
                    PreviousSchool = s.PreviousSchool,
                    LastClass = s.LastClass,
                    Board = s.Board,
                    YearOfPassing = s.YearOfPassing,
                    MarksObtained = s.MarksObtained,
                    TotalMarks = s.TotalMarks,
                    Percentage = s.Percentage,
                    MonthlyFee = s.MonthlyFee,
                    AdmissionFee = s.AdmissionFee,
                    FeeCategory = s.FeeCategory,
                    Status = s.Status,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<StudentDto?> GetByAdmissionIdAsync(int admissionId)
        {
            return await _context.Students
                .Where(s => s.AdmissionId == admissionId)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    AdmissionId = s.AdmissionId,
                    RegistrationNo = s.RegistrationNo,
                    RollNo = s.RollNo,
                    AdmissionNo = s.AdmissionNo,
                    DateOfAdmission = s.DateOfAdmission,
                    CurrentClass = s.CurrentClass,
                    Section = s.Section,
                    NameOfStudent = s.NameOfStudent,
                    NameOfStudentUrdu = s.NameOfStudentUrdu,
                    FatherName = s.FatherName,
                    FatherNameUrdu = s.FatherNameUrdu,
                    FatherCNIC = s.FatherCNIC,
                    FatherOccupation = s.FatherOccupation,
                    FatherMobile = s.FatherMobile,
                    MotherName = s.MotherName,
                    MotherCNIC = s.MotherCNIC,
                    MotherMobile = s.MotherMobile,
                    GuardianName = s.GuardianName,
                    GuardianCNIC = s.GuardianCNIC,
                    GuardianRelation = s.GuardianRelation,
                    GuardianMobile = s.GuardianMobile,
                    DateOfBirth = s.DateOfBirth,
                    DateOfBirthInWords = s.DateOfBirthInWords,
                    PlaceOfBirth = s.PlaceOfBirth,
                    FormBNo = s.FormBNo,
                    Gender = s.Gender,
                    Religion = s.Religion,
                    BloodGroup = s.BloodGroup,
                    Nationality = s.Nationality,
                    StudentPhoto = s.StudentPhoto,
                    PresentAddress = s.PresentAddress,
                    PresentAddressUrdu = s.PresentAddressUrdu,
                    PermanentAddress = s.PermanentAddress,
                    PermanentAddressUrdu = s.PermanentAddressUrdu,
                    PhoneResidence = s.PhoneResidence,
                    EmergencyContact = s.EmergencyContact,
                    Email = s.Email,
                    PreviousSchool = s.PreviousSchool,
                    LastClass = s.LastClass,
                    Board = s.Board,
                    YearOfPassing = s.YearOfPassing,
                    MarksObtained = s.MarksObtained,
                    TotalMarks = s.TotalMarks,
                    Percentage = s.Percentage,
                    MonthlyFee = s.MonthlyFee,
                    AdmissionFee = s.AdmissionFee,
                    FeeCategory = s.FeeCategory,
                    Status = s.Status,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Models.Student> CreateAsync(CreateStudentDto dto)
        {
            var student = new Models.Student
            {
                AdmissionId = dto.AdmissionId,
                RegistrationNo = dto.RegistrationNo,
                RollNo = dto.RollNo,
                AdmissionNo = dto.AdmissionNo,
                DateOfAdmission = dto.DateOfAdmission,
                CurrentClass = dto.CurrentClass,
                Section = dto.Section,
                NameOfStudent = dto.NameOfStudent,
                NameOfStudentUrdu = dto.NameOfStudentUrdu,
                FatherName = dto.FatherName,
                FatherNameUrdu = dto.FatherNameUrdu,
                FatherCNIC = dto.FatherCNIC,
                FatherOccupation = dto.FatherOccupation,
                FatherMobile = dto.FatherMobile,
                MotherName = dto.MotherName,
                MotherCNIC = dto.MotherCNIC,
                MotherMobile = dto.MotherMobile,
                GuardianName = dto.GuardianName,
                GuardianCNIC = dto.GuardianCNIC,
                GuardianRelation = dto.GuardianRelation,
                GuardianMobile = dto.GuardianMobile,
                DateOfBirth = dto.DateOfBirth,
                DateOfBirthInWords = dto.DateOfBirthInWords,
                PlaceOfBirth = dto.PlaceOfBirth,
                FormBNo = dto.FormBNo,
                Gender = dto.Gender,
                Religion = dto.Religion,
                BloodGroup = dto.BloodGroup,
                Nationality = dto.Nationality,
                StudentPhoto = dto.StudentPhoto,
                PresentAddress = dto.PresentAddress,
                PresentAddressUrdu = dto.PresentAddressUrdu,
                PermanentAddress = dto.PermanentAddress,
                PermanentAddressUrdu = dto.PermanentAddressUrdu,
                PhoneResidence = dto.PhoneResidence,
                EmergencyContact = dto.EmergencyContact,
                Email = dto.Email,
                PreviousSchool = dto.PreviousSchool,
                LastClass = dto.LastClass,
                Board = dto.Board,
                YearOfPassing = dto.YearOfPassing,
                MarksObtained = dto.MarksObtained,
                TotalMarks = dto.TotalMarks,
                Percentage = dto.Percentage,
                MonthlyFee = dto.MonthlyFee,
                AdmissionFee = dto.AdmissionFee,
                FeeCategory = dto.FeeCategory,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return student;
        }

        public async Task<Models.Student> CreateFromAdmissionAsync(CreateStudentFromAdmissionDto dto)
        {
            var admission = await _context.Admissions.FindAsync(dto.AdmissionId);
            if (admission == null)
            {
                throw new ArgumentException("Admission not found");
            }

            var student = new Models.Student
            {
                AdmissionId = admission.Id,
                RegistrationNo = dto.RegistrationNo ?? admission.RegistrationNo,
                RollNo = dto.RollNo ?? admission.RollNo,
                AdmissionNo = admission.AdmissionNo,
                DateOfAdmission = admission.DateOfAdmission,
                CurrentClass = dto.CurrentClass ?? admission.ClassSought,
                Section = dto.Section,
                NameOfStudent = admission.NameOfStudent,
                NameOfStudentUrdu = admission.NameOfStudentUrdu,
                FatherName = admission.FatherName,
                FatherNameUrdu = admission.FatherNameUrdu,
                FatherCNIC = admission.FatherCNIC,
                FatherOccupation = admission.FatherOccupation,
                FatherMobile = admission.FatherMobile,
                MotherName = admission.MotherName,
                MotherCNIC = admission.MotherCNIC,
                MotherMobile = admission.MotherMobile,
                GuardianName = admission.GuardianName,
                GuardianCNIC = admission.GuardianCNIC,
                GuardianRelation = admission.GuardianRelation,
                GuardianMobile = admission.GuardianMobile,
                DateOfBirth = admission.DateOfBirth,
                DateOfBirthInWords = admission.DateOfBirthInWords,
                PlaceOfBirth = admission.PlaceOfBirth,
                FormBNo = admission.FormBNo,
                Gender = admission.Gender,
                Religion = admission.Religion,
                StudentPhoto = admission.StudentPhoto,
                PresentAddress = admission.PresentAddress,
                PresentAddressUrdu = admission.PresentAddressUrdu,
                PermanentAddress = admission.PermanentAddress,
                PermanentAddressUrdu = admission.PermanentAddressUrdu,
                PhoneResidence = admission.PhoneResidence,
                EmergencyContact = admission.EmergencyContact,
                PreviousSchool = admission.PreviousSchool,
                LastClass = admission.LastClass,
                Board = admission.Board,
                YearOfPassing = admission.YearOfPassing,
                MarksObtained = admission.MarksObtained,
                TotalMarks = admission.TotalMarks,
                Percentage = admission.Percentage,
                AdmissionFee = admission.AdmissionFee,
                MonthlyFee = admission.TuitionFee,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return student;
        }

        public async Task<Models.Student?> UpdateAsync(UpdateStudentDto dto)
        {
            var student = await _context.Students.FindAsync(dto.Id);
            if (student == null) return null;

            student.RegistrationNo = dto.RegistrationNo;
            student.RollNo = dto.RollNo;
            student.AdmissionNo = dto.AdmissionNo;
            student.DateOfAdmission = dto.DateOfAdmission;
            student.CurrentClass = dto.CurrentClass;
            student.Section = dto.Section;
            student.NameOfStudent = dto.NameOfStudent;
            student.NameOfStudentUrdu = dto.NameOfStudentUrdu;
            student.FatherName = dto.FatherName;
            student.FatherNameUrdu = dto.FatherNameUrdu;
            student.FatherCNIC = dto.FatherCNIC;
            student.FatherOccupation = dto.FatherOccupation;
            student.FatherMobile = dto.FatherMobile;
            student.MotherName = dto.MotherName;
            student.MotherCNIC = dto.MotherCNIC;
            student.MotherMobile = dto.MotherMobile;
            student.GuardianName = dto.GuardianName;
            student.GuardianCNIC = dto.GuardianCNIC;
            student.GuardianRelation = dto.GuardianRelation;
            student.GuardianMobile = dto.GuardianMobile;
            student.DateOfBirth = dto.DateOfBirth;
            student.DateOfBirthInWords = dto.DateOfBirthInWords;
            student.PlaceOfBirth = dto.PlaceOfBirth;
            student.FormBNo = dto.FormBNo;
            student.Gender = dto.Gender;
            student.Religion = dto.Religion;
            student.BloodGroup = dto.BloodGroup;
            student.Nationality = dto.Nationality;
            student.StudentPhoto = dto.StudentPhoto;
            student.PresentAddress = dto.PresentAddress;
            student.PresentAddressUrdu = dto.PresentAddressUrdu;
            student.PermanentAddress = dto.PermanentAddress;
            student.PermanentAddressUrdu = dto.PermanentAddressUrdu;
            student.PhoneResidence = dto.PhoneResidence;
            student.EmergencyContact = dto.EmergencyContact;
            student.Email = dto.Email;
            student.PreviousSchool = dto.PreviousSchool;
            student.LastClass = dto.LastClass;
            student.Board = dto.Board;
            student.YearOfPassing = dto.YearOfPassing;
            student.MarksObtained = dto.MarksObtained;
            student.TotalMarks = dto.TotalMarks;
            student.Percentage = dto.Percentage;
            student.MonthlyFee = dto.MonthlyFee;
            student.AdmissionFee = dto.AdmissionFee;
            student.FeeCategory = dto.FeeCategory;
            student.Status = dto.Status;
            student.IsActive = dto.IsActive;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return student;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByAdmissionIdAsync(int admissionId)
        {
            return await _context.Students.AnyAsync(s => s.AdmissionId == admissionId);
        }
    }
}
