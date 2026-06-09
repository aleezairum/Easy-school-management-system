using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.Data.DBModels.FeeManagement;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class FeeVoucherRepository : IFeeVoucherRepository
    {
        private readonly SchoolDbContext _context;

        public FeeVoucherRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<FeeVoucherMain?> GetByIdAsync(int id)
        {
            var result = await _context
                .Set<FeeVoucherMain>()
                .FromSqlRaw("EXEC SpGet_FeeVoucherMain @VID={0}", id)
                .AsNoTracking()
                .ToListAsync();
            return result.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(FeeVoucherSaveDto dto, int userId, string userIp)
        {
            // 1. Save Main Voucher
            var mainResult = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_FeeVoucherMain @VID={0}, @VNo={1}, @VDate={2}, @PaymentMethodID={3}, @ParentID={4}, @BalanceAmount={5}, @PaidAmount={6}, @Remarks={7}, @UserID={8}, @UserIP={9}",
                    dto.VID,
                    dto.VNo ?? "",
                    dto.VDate,
                    dto.PaymentMethodID,
                    dto.ParentID,
                    dto.BalanceAmount,
                    dto.PaidAmount,
                    dto.Remarks ?? "",
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            var main = mainResult.FirstOrDefault();
            if (main == null || main.ReturnCode != 200)
                return main ?? new ResponseDto { ReturnCode = 500, ReturnMessage = "Failed to save voucher" };

            decimal mid = main.VID;

            // 2. Save each detail line
            foreach (var detail in dto.Details)
            {
                await _context
                    .Set<ResponseDto>()
                    .FromSqlRaw(
                        "EXEC SpSave_FeeVoucherDetail @VID={0}, @MID={1}, @StudentFeeID={2}, @PendingBalance={3}, @PaidAmount={4}, @UserID={5}, @UserIP={6}",
                        detail.VID,
                        mid,
                        detail.StudentFeeID,
                        detail.PendingBalance,
                        detail.PaidAmount,
                        userId,
                        userIp)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return main;
        }
        public async Task<string> GetNextVNoAsync()
        {
            // SP returns single row with VNo column
            var result = await _context
                .Set<VNoDto>()
                .FromSqlRaw("EXEC SpGetNextVNo_FeeVoucherMain")
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault()?.VNo ?? "";
        }

        public async Task<FeeVoucherReceiptDto?> GetReceiptAsync(decimal id)
        {
            var rows = await _context
                .Set<FeeVoucherRptRow>()
                .FromSqlRaw("EXEC SpRPT_FeeVoucher @VID={0}", id)
                .AsNoTracking()
                .ToListAsync();

            if (!rows.Any()) return null;

            var first = rows.First();

            return new FeeVoucherReceiptDto
            {
                VID = first.VID,
                VNo = first.VNo,
                VDate = first.VDate,
                PaymentMethod = first.PaymentMethod,
                ParentName = first.ParentName,
                BalanceAmount = first.BalanceAmount,
                PaidAmount = first.PaidAmount,
                Remarks = first.Remarks,
                UserFullName = first.UserFullName,
                Details = rows.Select(r => new FeeVoucherReceiptDetailDto
                {
                    StudentFeeID = r.StudentFeeID,
                    StudentName = r.StudentName,
                    SectionName = r.SectionName,
                    FeeTypeName = r.FeeTypeName,
                    FeeMonth = r.FeeMonth,
                    FeeAmount = r.FeeAmount,
                    PaidAmount = r.DetailPaidAmount,
                    PendingBalance = r.PendingBalance
                }).ToList()
            };
        }
        public async Task<List<FeeVoucherListDto>> GetAllAsync(int? classId, int? sectionId, int? studentId, DateTime dateFrom, DateTime dateTo)
        {
            var rows = await _context.Set<FeeVoucherListDto>()
                .FromSqlRaw(
                    "EXEC SpGet_FeeVoucherMain @ClassID={0}, @SectionID={1}, @StudentID={2}, @DateFrom={3}, @DateTo={4}",
                    classId.HasValue ? (object)classId.Value : DBNull.Value,
                    sectionId.HasValue ? (object)sectionId.Value : DBNull.Value,
                    studentId.HasValue ? (object)studentId.Value : DBNull.Value,
                    dateFrom, dateTo)
                .AsNoTracking()
                .ToListAsync();

            // Group by VID — ek voucher ke multiple detail rows ho sakti hain
            return rows.GroupBy(r => r.VID).Select(g =>
            {
                var first = g.First();
                return new FeeVoucherListDto
                {
                    VID = first.VID,
                    VNo = first.VNo,
                    VDate = first.VDate,
                    PaymentMethod = first.PaymentMethod,
                    ParentID = first.ParentID,
                    ParentName = first.ParentName,
                    BalanceAmount = first.BalanceAmount,
                    PaidAmount = first.PaidAmount,
                    Remarks = first.Remarks,
                    UserFullName = first.UserFullName,
                    StudentID = first.StudentID,
                    StudentName = first.StudentName,
                    SectionName = first.SectionName,
                };
            }).ToList();
        }

        public async Task<ResponseDto> DeleteByIdAsync(int id, int userId, string userIp)
        {
            var result = await _context.Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpDelete_FeeVoucherMain @VID={0}, @UserID={1}, @UserIP={2}",
                    id, userId, userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault() ?? new ResponseDto
            {
                ReturnCode = 500,
                ReturnMessage = "Unknown error"
            };
        }

    }
}