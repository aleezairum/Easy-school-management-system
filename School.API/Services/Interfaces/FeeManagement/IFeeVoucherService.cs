using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IFeeVoucherService
    {
        Task<List<FeeVoucherListDto>> GetAllAsync(int? classId, int? sectionId, int? studentId, DateTime dateFrom, DateTime dateTo);
        Task<FeeVoucherMain?> GetByIdAsync(int id);
        Task<ResponseDto> DeleteByIdAsync(int id, int userId, string userIp);
        Task<ResponseDto> SaveAsync(FeeVoucherSaveDto dto, int userId, string userIp);
        Task<string> GetNextVNoAsync();
        Task<FeeVoucherReceiptDto?> GetReceiptAsync(decimal id);

        /// <summary>
        /// The printed A4 (or thermal) voucher, as PDF bytes.
        /// </summary>
        /// <remarks>
        /// Lives here rather than in the controller so the WhatsApp auto-reply
        /// sends parents the EXACT voucher the counter prints — one RDLC, one
        /// set of parameters, no second implementation to drift out of step.
        /// Returns null when the voucher does not exist.
        /// </remarks>
        Task<(byte[] Pdf, string FileName)?> GetReceiptPdfAsync(decimal id, string type = "a4");
    }
}