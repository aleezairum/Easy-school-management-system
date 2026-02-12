using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly SchoolDbContext _context;

        public PaymentMethodRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentMethodListDto>> GetAllAsync()
        {
            return await _context.PaymentMethods
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentMethodListDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    BankName = p.BankName,
                    IsActive = p.IsActive
                })
                .ToListAsync();
        }

        public async Task<PaymentMethodDto?> GetByIdAsync(int id)
        {
            return await _context.PaymentMethods
                .Where(p => p.Id == id)
                .Select(p => new PaymentMethodDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    BankName = p.BankName,
                    AccountNumber = p.AccountNumber,
                    BranchCode = p.BranchCode,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PaymentMethod> CreateAsync(CreatePaymentMethodDto dto)
        {
            var entity = new PaymentMethod
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                BranchCode = dto.BranchCode,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<PaymentMethod?> UpdateAsync(int id, UpdatePaymentMethodDto dto)
        {
            var entity = await _context.PaymentMethods.FindAsync(id);
            if (entity == null)
                return null;

            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.BankName = dto.BankName;
            entity.AccountNumber = dto.AccountNumber;
            entity.BranchCode = dto.BranchCode;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.PaymentMethods.FindAsync(id);
            if (entity == null)
                return false;

            _context.PaymentMethods.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PaymentMethodDropdownDto>> GetDropdownAsync()
        {
            return await _context.PaymentMethods
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new PaymentMethodDropdownDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.PaymentMethods.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _context.PaymentMethods.Where(p => p.Code == code);
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}
