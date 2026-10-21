using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.SMS;
using School.API.Extensions;
using School.API.Services.Interfaces;

namespace School.API.Controllers
{
    /// <summary>
    /// School announcements - given on the WhatsApp menu ("Announcements")
    /// while they run.
    /// </summary>
    [ApiController]
    [Route("api/announcement")]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _service;

        public AnnouncementController(IAnnouncementService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<AnnouncementDto>>> GetAll() => Ok(await _service.GetAllAsync());

        // Open: the push notification shows this picture, and it is a school notice anyway.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpGet("{id:int}/image")]
        public async Task<ActionResult> GetImage(int id)
        {
            var a = await _service.FindAsync(id);
            return a?.ImageData == null ? NotFound() : File(a.ImageData, a.ImageContentType ?? "image/jpeg", a.ImageFileName);
        }

        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public Task<ActionResult<AnnouncementDto>> Create(SaveAnnouncementDto dto) => Save(null, dto);

        [HttpPut("{id:int}")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public Task<ActionResult<AnnouncementDto>> Update(int id, SaveAnnouncementDto dto) => Save(id, dto);

        private async Task<ActionResult<AnnouncementDto>> Save(int? id, SaveAnnouncementDto dto)
        {
            var userId = Request.GetUserId();
            try
            {
                return Ok(await _service.SaveAsync(id, dto, userId == 0 ? null : userId));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id) =>
            await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
