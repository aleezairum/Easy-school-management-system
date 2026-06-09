using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
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

        public async Task<List<PaymentMethod>> GetAllAsync()
        {
            var resultList = await _context
                .Set<PaymentMethod>()
                .FromSqlRaw("EXEC SpGet_PaymentMethod @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<PaymentMethod?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<PaymentMethod>()
                .FromSqlRaw("EXEC SpGet_PaymentMethod @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_PaymentMethod @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(PaymentMethodDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_PaymentMethod @VID={0}, @VCode={1}, @VName={2}, @Description={3}, @IsActive={4}, @UserID={5}, @UserIP={6}",
                    dto.VID,
                    dto.VCode,
                    dto.VName,
                    dto.Description,
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync(); // materialize results in memory

            // Safely get the first (or single) record
            var result = resultList.FirstOrDefault();

            return result;
        }
    }
}
