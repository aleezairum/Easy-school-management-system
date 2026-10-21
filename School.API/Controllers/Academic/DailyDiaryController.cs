using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    /// <summary>
    /// Class and section wise daily diary - photographed pages, sent to parents
    /// on WhatsApp ("Today's Diary").
    /// </summary>
    [ApiController]
    [Route("api/daily-diary")]
    public class DailyDiaryController : ControllerBase
    {
        private readonly IDailyDiaryService _service;

        public DailyDiaryController(IDailyDiaryService service)
        {
            _service = service;
        }

        /// <param name="date">One day; leave out for every day.</param>
        [HttpGet]
        public async Task<ActionResult<List<DailyDiaryDto>>> Get([FromQuery] DateTime? date, [FromQuery] int? classId) =>
            Ok(await _service.GetAsync(date, classId));

        /// <summary>Adds pictures (and the note) to the day's diary for the class and section.</summary>
        [HttpPost]
        [RequestSizeLimit(60 * 1024 * 1024)]
        public async Task<ActionResult<DailyDiaryDto>> Save(SaveDailyDiaryDto dto)
        {
            var userId = Request.GetUserId();
            try
            {
                return Ok(await _service.SaveAsync(dto, userId == 0 ? null : userId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>One page, as the picture itself - for the screen's thumbnails
        /// and preview. Sent inline (no download name) so the browser shows it
        /// instead of saving it as a file.</summary>
        [HttpGet("image/{imageId:int}")]
        public async Task<ActionResult> GetImage(int imageId)
        {
            var image = await _service.GetImageAsync(imageId);
            return image == null ? NotFound() : File(image.ImageData, image.ContentType);
        }

        [HttpDelete("image/{imageId:int}")]
        public async Task<ActionResult> DeleteImage(int imageId) =>
            await _service.DeleteImageAsync(imageId) ? NoContent() : NotFound();

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id) =>
            await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
