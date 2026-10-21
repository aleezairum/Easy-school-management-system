using Microsoft.AspNetCore.Mvc;
using School.API.Services.Interfaces;

namespace School.API.Controllers
{
    /// <summary>
    /// What the WhatsApp auto-reply WOULD send to a number for a message -
    /// nothing is sent. For checking the bot on a development machine.
    /// </summary>
    /// <remarks>
    /// Development only: on any other environment it answers 404, because it
    /// would otherwise show anyone a family's or a member of staff's records
    /// for any number they typed in.
    /// </remarks>
    [ApiController]
    [Route("api/whatsapp-bot/preview")]
    public class WhatsAppBotPreviewController : ControllerBase
    {
        private readonly IWhatsAppBotService _bot;
        private readonly IWebHostEnvironment _env;

        public WhatsAppBotPreviewController(IWhatsAppBotService bot, IWebHostEnvironment env)
        {
            _bot = bot;
            _env = env;
        }

        /// <param name="from">The sender's number, any format (0300..., 92300...).</param>
        /// <param name="text">The message, e.g. "hi" or a menu number.</param>
        /// <param name="pdf">Include each PDF as base64.</param>
        [HttpGet]
        public async Task<ActionResult> Preview([FromQuery] string from, [FromQuery] string? text, [FromQuery] bool pdf = false)
        {
            if (!_env.IsDevelopment()) return NotFound();

            var replies = await _bot.ComposeReplyAsync(from, text);
            return Ok(replies.Select(r => new
            {
                r.Text,
                Menu = r.Interactive == null ? null : new
                {
                    r.Interactive.ButtonText,
                    r.Interactive.Footer,
                    Rows = r.Interactive.Rows.Select(x => new { x.Id, x.Title, x.Description })
                },
                r.DocumentFileName,
                DocumentBytes = r.DocumentBytes?.Length ?? 0,
                Pdf = pdf && r.DocumentBytes != null ? Convert.ToBase64String(r.DocumentBytes) : null
            }));
        }
    }
}
