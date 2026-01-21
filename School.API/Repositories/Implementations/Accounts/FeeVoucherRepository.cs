using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class FeeVoucherRepository : IFeeVoucherRepository
    {
        private readonly SchoolDbContext _context;
        private readonly IChallanVoucherRepository _challanRepository;

        public FeeVoucherRepository(SchoolDbContext context, IChallanVoucherRepository challanRepository)
        {
            _context = context;
            _challanRepository = challanRepository;
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetAllAsync()
        {
            return await _context.FeeVouchers
                .Include(f => f.ChallanVoucher)
                    .ThenInclude(c => c!.Student)
                .Include(f => f.PaymentMethod)
                .OrderByDescending(f => f.PaymentDate)
                .Select(f => new FeeVoucherListDto
                {
                    Id = f.Id,
                    VoucherNumber = f.VoucherNumber,
                    ChallanNumber = f.ChallanVoucher != null ? f.ChallanVoucher.ChallanNumber : null,
                    StudentName = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.NameOfStudent : null,
                    RollNumber = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.RollNo : null,
                    PaymentMethodName = f.PaymentMethod != null ? f.PaymentMethod.Name : null,
                    PaymentDate = f.PaymentDate,
                    AmountPaid = f.AmountPaid,
                    TransactionReference = f.TransactionReference
                })
                .ToListAsync();
        }

        public async Task<FeeVoucherDto?> GetByIdAsync(int id)
        {
            return await _context.FeeVouchers
                .Include(f => f.ChallanVoucher)
                    .ThenInclude(c => c!.Student)
                .Include(f => f.PaymentMethod)
                .Where(f => f.Id == id)
                .Select(f => new FeeVoucherDto
                {
                    Id = f.Id,
                    VoucherNumber = f.VoucherNumber,
                    ChallanVoucherId = f.ChallanVoucherId,
                    ChallanNumber = f.ChallanVoucher != null ? f.ChallanVoucher.ChallanNumber : null,
                    StudentName = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.NameOfStudent : null,
                    RollNumber = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.RollNo : null,
                    PaymentMethodId = f.PaymentMethodId,
                    PaymentMethodName = f.PaymentMethod != null ? f.PaymentMethod.Name : null,
                    PaymentDate = f.PaymentDate,
                    AmountPaid = f.AmountPaid,
                    ChequeNumber = f.ChequeNumber,
                    ChequeDate = f.ChequeDate,
                    BankName = f.BankName,
                    TransactionReference = f.TransactionReference,
                    Remarks = f.Remarks,
                    ReceivedBy = f.ReceivedBy,
                    CreatedAt = f.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetByChallanAsync(int challanVoucherId)
        {
            return await _context.FeeVouchers
                .Include(f => f.ChallanVoucher)
                    .ThenInclude(c => c!.Student)
                .Include(f => f.PaymentMethod)
                .Where(f => f.ChallanVoucherId == challanVoucherId)
                .OrderByDescending(f => f.PaymentDate)
                .Select(f => new FeeVoucherListDto
                {
                    Id = f.Id,
                    VoucherNumber = f.VoucherNumber,
                    ChallanNumber = f.ChallanVoucher != null ? f.ChallanVoucher.ChallanNumber : null,
                    StudentName = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.NameOfStudent : null,
                    RollNumber = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.RollNo : null,
                    PaymentMethodName = f.PaymentMethod != null ? f.PaymentMethod.Name : null,
                    PaymentDate = f.PaymentDate,
                    AmountPaid = f.AmountPaid,
                    TransactionReference = f.TransactionReference
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.FeeVouchers
                .Include(f => f.ChallanVoucher)
                    .ThenInclude(c => c!.Student)
                .Include(f => f.PaymentMethod)
                .Where(f => f.PaymentDate >= fromDate.Date && f.PaymentDate <= toDate.Date)
                .OrderByDescending(f => f.PaymentDate)
                .Select(f => new FeeVoucherListDto
                {
                    Id = f.Id,
                    VoucherNumber = f.VoucherNumber,
                    ChallanNumber = f.ChallanVoucher != null ? f.ChallanVoucher.ChallanNumber : null,
                    StudentName = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.NameOfStudent : null,
                    RollNumber = f.ChallanVoucher != null && f.ChallanVoucher.Student != null
                        ? f.ChallanVoucher.Student.RollNo : null,
                    PaymentMethodName = f.PaymentMethod != null ? f.PaymentMethod.Name : null,
                    PaymentDate = f.PaymentDate,
                    AmountPaid = f.AmountPaid,
                    TransactionReference = f.TransactionReference
                })
                .ToListAsync();
        }

        public async Task<FeeVoucher> CreateAsync(CreateFeeVoucherDto dto)
        {
            var entity = new FeeVoucher
            {
                VoucherNumber = await GenerateVoucherNumberAsync(),
                ChallanVoucherId = dto.ChallanVoucherId,
                PaymentMethodId = dto.PaymentMethodId,
                PaymentDate = dto.PaymentDate.Date,
                AmountPaid = dto.AmountPaid,
                ChequeNumber = dto.ChequeNumber,
                ChequeDate = dto.ChequeDate,
                BankName = dto.BankName,
                TransactionReference = dto.TransactionReference,
                Remarks = dto.Remarks,
                ReceivedBy = dto.ReceivedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeeVouchers.Add(entity);
            await _context.SaveChangesAsync();

            // Update the challan's paid amount
            await _challanRepository.UpdatePaidAmountAsync(dto.ChallanVoucherId, dto.AmountPaid);

            return entity;
        }

        public async Task<FeeVoucher?> UpdateAsync(int id, UpdateFeeVoucherDto dto)
        {
            var entity = await _context.FeeVouchers.FindAsync(id);
            if (entity == null)
                return null;

            var oldAmount = entity.AmountPaid;
            var oldChallanId = entity.ChallanVoucherId;

            entity.ChallanVoucherId = dto.ChallanVoucherId;
            entity.PaymentMethodId = dto.PaymentMethodId;
            entity.PaymentDate = dto.PaymentDate.Date;
            entity.AmountPaid = dto.AmountPaid;
            entity.ChequeNumber = dto.ChequeNumber;
            entity.ChequeDate = dto.ChequeDate;
            entity.BankName = dto.BankName;
            entity.TransactionReference = dto.TransactionReference;
            entity.Remarks = dto.Remarks;
            entity.ReceivedBy = dto.ReceivedBy;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // If amount or challan changed, update the challan's paid amount
            if (oldChallanId != dto.ChallanVoucherId || oldAmount != dto.AmountPaid)
            {
                // Revert old challan's paid amount
                await _challanRepository.UpdatePaidAmountAsync(oldChallanId, -oldAmount);
                // Update new challan's paid amount
                await _challanRepository.UpdatePaidAmountAsync(dto.ChallanVoucherId, dto.AmountPaid);
            }

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.FeeVouchers.FindAsync(id);
            if (entity == null)
                return false;

            // Revert the challan's paid amount
            await _challanRepository.UpdatePaidAmountAsync(entity.ChallanVoucherId, -entity.AmountPaid);

            _context.FeeVouchers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.FeeVouchers.AnyAsync(f => f.Id == id);
        }

        public async Task<string> GenerateVoucherNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"FV-{today:yyyyMMdd}-";

            var lastVoucher = await _context.FeeVouchers
                .Where(f => f.VoucherNumber.StartsWith(prefix))
                .OrderByDescending(f => f.VoucherNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastVoucher != null)
            {
                var lastNumberStr = lastVoucher.VoucherNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}
