using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.NETCore;
using School.API.DTOs.FeeManagement;
using School.API.Extensions;
using School.API.Services.Interfaces.Accounts;

[ApiController]
[Route("api/[controller]")]
public class FeeVoucherController : ControllerBase
{
    private readonly IFeeVoucherService _service;

    public FeeVoucherController(IFeeVoucherService service)
    {
        _service = service;
    }

    

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _service.GetByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    

    [HttpPost]
    public async Task<IActionResult> Save(FeeVoucherSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
    [HttpGet("NextVNo")]
    public async Task<IActionResult> GetNextVNo()
    {
        var vno = await _service.GetNextVNoAsync();
        return Ok(new { vNo = vno });
    }
    [HttpGet("Receipt/{id}")]
    public async Task<IActionResult> GetReceipt(decimal id)
    {
        var receipt = await _service.GetReceiptAsync(id);
        return receipt == null ? NotFound() : Ok(receipt);
    }

    [HttpGet("ReceiptPdf/{id}")]
    public async Task<IActionResult> GetReceiptPdf(decimal id, [FromQuery] string type = "a4")
    {
        var receipt = await _service.GetReceiptAsync(id);
        if (receipt == null) return NotFound();

        var totalPaid = receipt.Details.Sum(d => d.PaidAmount);
        var remaining = Math.Max(0, receipt.BalanceAmount - totalPaid);

        var rdlcFile = type.ToLower() == "thermal" ? "FeeVoucherThermal.rdlc" : "FeeVoucherA4.rdlc";
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", rdlcFile);
        using var report = new LocalReport();
        report.ReportPath = reportPath;

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
            new ReportParameter("VDate", $"{receipt.VDate:dd-MM-yyyy} {receipt.InsertedDate:hh:mm tt}"),
            new ReportParameter("PaymentMethod", receipt.PaymentMethod ?? "-"),
            new ReportParameter("ParentName", receipt.ParentName ?? "-"),
            new ReportParameter("TotalPaid", totalPaid.ToString("N0")),
            new ReportParameter("RemainingDues", remaining.ToString("N0")),
            new ReportParameter("ReceivedBy", receipt.UserFullName ?? "-")
        });

        var pdfBytes = report.Render("PDF");
        return File(pdfBytes, "application/pdf", $"FeeVoucher_{receipt.VNo}.pdf");
    }
    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] int? classId = null,
    [FromQuery] int? sectionId = null,
    [FromQuery] int? studentId = null,
    [FromQuery] DateTime? dateFrom = null,
    [FromQuery] DateTime? dateTo = null)
    {
        var from = dateFrom ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var to = dateTo ?? DateTime.Now;

        var data = await _service.GetAllAsync(classId, sectionId, studentId, from, to);
        return Ok(data);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = Request.GetUserId();
        var result = await _service.DeleteByIdAsync(id, userId, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        return result.ReturnCode == 200 ? Ok(result) : BadRequest(result);
    }
}