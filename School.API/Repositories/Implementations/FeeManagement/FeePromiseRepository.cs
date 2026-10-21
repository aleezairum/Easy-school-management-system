using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    /// <summary>
    /// Promises to pay — see Scripts/FeePromise.sql for the rule they enforce.
    /// </summary>
    public class FeePromiseRepository : IFeePromiseRepository
    {
        private readonly SchoolDbContext _context;

        public FeePromiseRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<FeePromiseDto>> GetAsync(
            int? vid, int? studentId, string? classIds, string? campusIds, byte status)
        {
            return await _context
                .Set<FeePromiseDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSFeePromise @VID={0}, @StudentID={1}, @ClassIDs={2}, @CampusIDs={3}, @Status={4}",
                    (object?)vid ?? DBNull.Value,
                    (object?)studentId ?? DBNull.Value,
                    string.IsNullOrWhiteSpace(classIds) ? (object)DBNull.Value : classIds,
                    string.IsNullOrWhiteSpace(campusIds) ? (object)DBNull.Value : campusIds,
                    status)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// The printed report: a row per student covered, siblings included.
        /// </summary>
        public async Task<List<FeePromiseReportDto>> GetReportAsync(
            string? classIds, string? campusIds, byte status)
        {
            return await _context
                .Set<FeePromiseReportDto>()
                .FromSqlRaw(
                    "EXEC SpRPT_SMSFeePromise @Status={0}, @ClassIDs={1}, @CampusIDs={2}",
                    status,
                    string.IsNullOrWhiteSpace(classIds) ? (object)DBNull.Value : classIds,
                    string.IsNullOrWhiteSpace(campusIds) ? (object)DBNull.Value : campusIds)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResponseDto?> SaveAsync(SaveFeePromiseDto dto, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSFeePromise @VID={0}, @StudentID={1}, @PromiseDate={2}, @PromisedAmount={3}, " +
                    "@PromisedBy={4}, @Remarks={5}, @IsActive={6}, @UserID={7}, @UserIP={8}",
                    dto.VID,
                    dto.StudentID,
                    dto.PromiseDate.Date,
                    (object?)dto.PromisedAmount ?? DBNull.Value,
                    string.IsNullOrWhiteSpace(dto.PromisedBy) ? (object)DBNull.Value : dto.PromisedBy.Trim(),
                    string.IsNullOrWhiteSpace(dto.Remarks) ? (object)DBNull.Value : dto.Remarks.Trim(),
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteAsync(int vid, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSFeePromise @VID={0}, @UserID={1}, @UserIP={2}", vid, userId, userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }
    }
}
