using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class ChallanVoucherRepository : IChallanVoucherRepository
    {
        private readonly SchoolDbContext _context;

        public ChallanVoucherRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetAllAsync()
        {
            return await _context.ChallanVouchers
                .Include(c => c.Student)
                .Include(c => c.Class)
                .Include(c => c.Section)
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new ChallanVoucherListDto
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNumber,
                    StudentName = c.Student != null ? c.Student.NameOfStudent : null,
                    RollNumber = c.Student != null ? c.Student.RollNo : null,
                    ClassName = c.Class != null ? c.Class.VName : null,
                    SectionName = c.Section != null ? c.Section.VName : null,
                    IssueDate = c.IssueDate,
                    DueDate = c.DueDate,
                    TotalAmount = c.TotalAmount,
                    PaidAmount = c.PaidAmount,
                    BalanceAmount = c.TotalAmount - c.PaidAmount,
                    Status = c.Status,
                    StatusName = c.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<ChallanVoucherDto?> GetByIdAsync(int id)
        {
            return await _context.ChallanVouchers
                .Include(c => c.Student)
                .Include(c => c.Class)
                .Include(c => c.Section)
                .Include(c => c.Session)
                .Where(c => c.Id == id)
                .Select(c => new ChallanVoucherDto
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNumber,
                    StudentId = c.StudentId,
                    StudentName = c.Student != null ? c.Student.NameOfStudent : null,
                    RollNumber = c.Student != null ? c.Student.RollNo : null,
                    ClassId = c.ClassId,
                    ClassName = c.Class != null ? c.Class.VName : null,
                    SectionId = c.SectionId,
                    SectionName = c.Section != null ? c.Section.VName : null,
                    SessionId = c.SessionId,
                    SessionName = c.Session != null ? c.Session.VName : null,
                    IssueDate = c.IssueDate,
                    DueDate = c.DueDate,
                    TuitionFee = c.TuitionFee,
                    AdmissionFee = c.AdmissionFee,
                    ExamFee = c.ExamFee,
                    TransportFee = c.TransportFee,
                    LabFee = c.LabFee,
                    LibraryFee = c.LibraryFee,
                    SportsFee = c.SportsFee,
                    ComputerFee = c.ComputerFee,
                    OtherFee = c.OtherFee,
                    ArrearsAmount = c.ArrearsAmount,
                    Discount = c.Discount,
                    LateFee = c.LateFee,
                    GrossAmount = c.TuitionFee + c.AdmissionFee + c.ExamFee + c.TransportFee +
                                  c.LabFee + c.LibraryFee + c.SportsFee + c.ComputerFee +
                                  c.OtherFee + c.ArrearsAmount,
                    TotalAmount = c.TotalAmount,
                    PaidAmount = c.PaidAmount,
                    BalanceAmount = c.TotalAmount - c.PaidAmount,
                    Status = c.Status,
                    Remarks = c.Remarks,
                    ForMonth = c.ForMonth,
                    ForYear = c.ForYear,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetByStudentAsync(int studentId)
        {
            return await _context.ChallanVouchers
                .Include(c => c.Student)
                .Include(c => c.Class)
                .Include(c => c.Section)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new ChallanVoucherListDto
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNumber,
                    StudentName = c.Student != null ? c.Student.NameOfStudent : null,
                    RollNumber = c.Student != null ? c.Student.RollNo : null,
                    ClassName = c.Class != null ? c.Class.VName : null,
                    SectionName = c.Section != null ? c.Section.VName : null,
                    IssueDate = c.IssueDate,
                    DueDate = c.DueDate,
                    TotalAmount = c.TotalAmount,
                    PaidAmount = c.PaidAmount,
                    BalanceAmount = c.TotalAmount - c.PaidAmount,
                    Status = c.Status,
                    StatusName = c.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId)
        {
            return await _context.ChallanVouchers
                .Include(c => c.Student)
                .Where(c => c.ClassId == classId && c.SectionId == sectionId && c.SessionId == sessionId)
                .OrderBy(c => c.Student!.RollNo)
                .Select(c => new ChallanVoucherListDto
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNumber,
                    StudentName = c.Student != null ? c.Student.NameOfStudent : null,
                    RollNumber = c.Student != null ? c.Student.RollNo : null,
                    ClassName = c.Class != null ? c.Class.VName : null,
                    SectionName = c.Section != null ? c.Section.VName : null,
                    IssueDate = c.IssueDate,
                    DueDate = c.DueDate,
                    TotalAmount = c.TotalAmount,
                    PaidAmount = c.PaidAmount,
                    BalanceAmount = c.TotalAmount - c.PaidAmount,
                    Status = c.Status,
                    StatusName = c.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ChallanDropdownDto>> GetPendingByStudentAsync(int studentId)
        {
            return await _context.ChallanVouchers
                .Include(c => c.Student)
                .Where(c => c.StudentId == studentId &&
                            (c.Status == ChallanStatus.Pending || c.Status == ChallanStatus.PartiallyPaid || c.Status == ChallanStatus.Overdue))
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new ChallanDropdownDto
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNumber,
                    StudentName = c.Student != null ? c.Student.NameOfStudent : null,
                    BalanceAmount = c.TotalAmount - c.PaidAmount
                })
                .ToListAsync();
        }

        public async Task<ChallanVoucher> CreateAsync(CreateChallanVoucherDto dto)
        {
            var grossAmount = dto.TuitionFee + dto.AdmissionFee + dto.ExamFee + dto.TransportFee +
                              dto.LabFee + dto.LibraryFee + dto.SportsFee + dto.ComputerFee +
                              dto.OtherFee + dto.ArrearsAmount;
            var totalAmount = grossAmount - dto.Discount + dto.LateFee;

            var entity = new ChallanVoucher
            {
                ChallanNumber = await GenerateChallanNumberAsync(),
                StudentId = dto.StudentId,
                ClassId = dto.ClassId,
                SectionId = dto.SectionId,
                SessionId = dto.SessionId,
                IssueDate = dto.IssueDate.Date,
                DueDate = dto.DueDate.Date,
                TuitionFee = dto.TuitionFee,
                AdmissionFee = dto.AdmissionFee,
                ExamFee = dto.ExamFee,
                TransportFee = dto.TransportFee,
                LabFee = dto.LabFee,
                LibraryFee = dto.LibraryFee,
                SportsFee = dto.SportsFee,
                ComputerFee = dto.ComputerFee,
                OtherFee = dto.OtherFee,
                ArrearsAmount = dto.ArrearsAmount,
                Discount = dto.Discount,
                LateFee = dto.LateFee,
                TotalAmount = totalAmount,
                PaidAmount = 0,
                Status = ChallanStatus.Pending,
                Remarks = dto.Remarks,
                ForMonth = dto.ForMonth,
                ForYear = dto.ForYear,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChallanVouchers.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ChallanVoucher?> UpdateAsync(int id, UpdateChallanVoucherDto dto)
        {
            var entity = await _context.ChallanVouchers.FindAsync(id);
            if (entity == null)
                return null;

            var grossAmount = dto.TuitionFee + dto.AdmissionFee + dto.ExamFee + dto.TransportFee +
                              dto.LabFee + dto.LibraryFee + dto.SportsFee + dto.ComputerFee +
                              dto.OtherFee + dto.ArrearsAmount;
            var totalAmount = grossAmount - dto.Discount + dto.LateFee;

            entity.StudentId = dto.StudentId;
            entity.ClassId = dto.ClassId;
            entity.SectionId = dto.SectionId;
            entity.SessionId = dto.SessionId;
            entity.IssueDate = dto.IssueDate.Date;
            entity.DueDate = dto.DueDate.Date;
            entity.TuitionFee = dto.TuitionFee;
            entity.AdmissionFee = dto.AdmissionFee;
            entity.ExamFee = dto.ExamFee;
            entity.TransportFee = dto.TransportFee;
            entity.LabFee = dto.LabFee;
            entity.LibraryFee = dto.LibraryFee;
            entity.SportsFee = dto.SportsFee;
            entity.ComputerFee = dto.ComputerFee;
            entity.OtherFee = dto.OtherFee;
            entity.ArrearsAmount = dto.ArrearsAmount;
            entity.Discount = dto.Discount;
            entity.LateFee = dto.LateFee;
            entity.TotalAmount = totalAmount;
            entity.Status = dto.Status;
            entity.Remarks = dto.Remarks;
            entity.ForMonth = dto.ForMonth;
            entity.ForYear = dto.ForYear;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ChallanVouchers.FindAsync(id);
            if (entity == null)
                return false;

            _context.ChallanVouchers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ChallanVouchers.AnyAsync(c => c.Id == id);
        }

        public async Task<string> GenerateChallanNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"CH-{today:yyyyMMdd}-";

            var lastChallan = await _context.ChallanVouchers
                .Where(c => c.ChallanNumber.StartsWith(prefix))
                .OrderByDescending(c => c.ChallanNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastChallan != null)
            {
                var lastNumberStr = lastChallan.ChallanNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task UpdatePaidAmountAsync(int id, decimal amount)
        {
            var entity = await _context.ChallanVouchers.FindAsync(id);
            if (entity == null)
                return;

            entity.PaidAmount += amount;

            if (entity.PaidAmount >= entity.TotalAmount)
            {
                entity.Status = ChallanStatus.Paid;
            }
            else if (entity.PaidAmount > 0)
            {
                entity.Status = ChallanStatus.PartiallyPaid;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
