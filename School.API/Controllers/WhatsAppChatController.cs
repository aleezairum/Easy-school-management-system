using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.Controllers
{
    // Two-way WhatsApp chat backing Views/SMS/Chat.cshtml.
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppChatController : ControllerBase
    {
        private readonly IWhatsAppChatService _chatService;

        public WhatsAppChatController(IWhatsAppChatService chatService)
        {
            _chatService = chatService;
        }

        // Chat list, most recent activity first.
        [HttpGet("conversations")]
        public async Task<ActionResult<List<WaConversationDto>>> GetConversations(
            [FromQuery] string? search, [FromQuery] int take = 100)
        {
            var conversations = await _chatService.GetConversationsAsync(search, take);
            return Ok(conversations);
        }

        // One page of a thread. Pass beforeVid to load older messages.
        [HttpGet("conversations/{waId}/messages")]
        public async Task<ActionResult<WaThreadDto>> GetThread(
            string waId, [FromQuery] int? beforeVid, [FromQuery] int take = 50)
        {
            var thread = await _chatService.GetThreadAsync(waId, beforeVid, take);
            if (thread is null)
                return NotFound(new { message = "No conversation with this contact yet" });

            return Ok(thread);
        }

        // Free-form reply. Rejected with 409 once the 24-hour window has closed.
        [HttpPost("conversations/{waId}/reply")]
        public async Task<ActionResult> Reply(string waId, [FromBody] WaReplyDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { message = "Message is required" });

            var (ok, error, message) = await _chatService.SendReplyAsync(waId, dto.Message);

            if (!ok)
                return Conflict(new { message = error, data = message });

            return Ok(message);
        }

        [HttpPost("conversations/{waId}/read")]
        public async Task<ActionResult> MarkRead(string waId)
        {
            var marked = await _chatService.MarkReadAsync(waId);
            return Ok(new { marked });
        }

        // Drives the unread badge in the sidebar/chat list.
        [HttpGet("unread-count")]
        public async Task<ActionResult> GetUnreadCount()
        {
            var unread = await _chatService.GetTotalUnreadAsync();
            return Ok(new { unread });
        }
    }
}
