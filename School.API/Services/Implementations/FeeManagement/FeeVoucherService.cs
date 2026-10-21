using Microsoft.Reporting.NETCore;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeeVoucherService : IFeeVoucherService
    {
        private readonly IFeeVoucherRepository _repo;

        public FeeVoucherService(IFeeVoucherRepository repo)
        {
            _repo = repo;
        }

        public Task<FeeVoucherMain?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<ResponseDto> SaveAsync(FeeVoucherSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);
        public Task<string> GetNextVNoAsync() => _repo.GetNextVNoAsync();
        public Task<FeeVoucherReceiptDto?> GetReceiptAsync(decimal id)
             => _repo.GetReceiptAsync(id);

        // Moved here from FeeVoucherController so the WhatsApp auto-reply can
        // send the same voucher the counter prints. The controller now calls
        // this too — there is only ever one place that fills the RDLC.
        public async Task<(byte[] Pdf, string FileName)?> GetReceiptPdfAsync(decimal id, string type = "a4")
        {
            var receipt = await _repo.GetReceiptAsync(id);
            if (receipt is null) return null;

            var totalPaid = receipt.Details.Sum(d => d.PaidAmount);
            var remaining = Math.Max(0, receipt.BalanceAmount - totalPaid);

            var rdlcFile = string.Equals(type, "thermal", StringComparison.OrdinalIgnoreCase)
                ? "FeeVoucherThermal.rdlc"
                : "FeeVoucherA4.rdlc";

            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", rdlcFile);
            using var report = new LocalReport { ReportPath = reportPath };

            var dataSource = receipt.Details.Select(d => new
            {
                d.StudentName,
                d.SectionName,
                d.FeeTypeName,
                d.FeeMonth,
                d.FeeAmount,
                d.PaidAmount,
                d.PendingBalance
            }).ToList();

            report.DataSources.Add(new ReportDataSource("FeeDetails", dataSource));
            report.SetParameters(new[]
            {
                new ReportParameter("VNo", receipt.VNo ?? "-"),
                // VDate is date-only; append the actual save time from InsertedDate.
                new ReportParameter("VDate", $"{receipt.VDate:dd-MMM-yyyy} {receipt.InsertedDate:hh:mm tt}"),
                new ReportParameter("PaymentMethod", receipt.PaymentMethod ?? "-"),
                new ReportParameter("ParentName", receipt.ParentName ?? "-"),
                new ReportParameter("TotalPaid", totalPaid.ToString("N0")),
                new ReportParameter("RemainingDues", remaining.ToString("N0")),
                new ReportParameter("ReceivedBy", receipt.UserFullName ?? "-")
            });

            return (report.Render("PDF"), $"FeeVoucher_{receipt.VNo}.pdf");
        }
        public Task<List<FeeVoucherListDto>> GetAllAsync(int? classId, int? sectionId, int? studentId, DateTime dateFrom, DateTime dateTo)
             => _repo.GetAllAsync(classId, sectionId, studentId, dateFrom, dateTo);

        public Task<ResponseDto> DeleteByIdAsync(int id, int userId, string userIp)
            => _repo.DeleteByIdAsync(id, userId, userIp);

    }
}