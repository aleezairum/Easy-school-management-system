using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        [HttpPost("send")]
        public async Task<ActionResult<SmsSendResponseDto>> Send([FromBody] SmsSendDto dto)
        {
            if (dto.Recipients == null || dto.Recipients.Count == 0)
                return BadRequest(new { message = "No recipients specified" });

            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { message = "Message is required" });

            var result = await _smsService.SendBulkAsync(dto);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult> GetHistory([FromQuery] SmsHistoryFilterDto filter)
        {
            var messages = await _smsService.GetHistoryAsync(filter);
            var total = await _smsService.GetHistoryCountAsync(filter);

            return Ok(new
            {
                data = messages,
                total,
                page = filter.Page,
                pageSize = filter.PageSize
            });
        }

        [HttpGet("absent-students")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetAbsentStudents([FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var students = await _smsService.GetAbsentStudentsAsync(targetDate);
            return Ok(students);
        }

        [HttpGet("late-students")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetLateStudents([FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var students = await _smsService.GetLateStudentsAsync(targetDate);
            return Ok(students);
        }

        [HttpGet("fee-defaulters")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetFeeDefaulters()
        {
            var students = await _smsService.GetFeeDefaultersAsync();
            return Ok(students);
        }
    }
}
