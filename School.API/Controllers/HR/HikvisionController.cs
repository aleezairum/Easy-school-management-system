using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    /// <summary>
    /// Face-attendance machines (Hikvision). The screen is Staff &amp; Payroll ->
    /// Attendance Machines; the machine itself posts to <c>event/{key}</c>.
    /// </summary>
    [ApiController]
    [Route("api/hikvision")]
    public class HikvisionController : ControllerBase
    {
        private readonly IHikvisionService _service;

        public HikvisionController(IHikvisionService service)
        {
            _service = service;
        }

        // ── Machines ───────────────────────────────────────────────────

        [HttpGet("devices")]
        public async Task<ActionResult<List<AttendanceDeviceDto>>> GetDevices()
        {
            try { return Ok(await _service.GetDevicesAsync()); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = TablesMissingMessage }); }
        }

        [HttpPost("devices")]
        public Task<ActionResult<AttendanceDeviceDto>> Create(SaveAttendanceDeviceDto dto) => Save(null, dto);

        [HttpPut("devices/{id:int}")]
        public Task<ActionResult<AttendanceDeviceDto>> Update(int id, SaveAttendanceDeviceDto dto) => Save(id, dto);

        private async Task<ActionResult<AttendanceDeviceDto>> Save(int? id, SaveAttendanceDeviceDto dto)
        {
            try { return Ok(await _service.SaveDeviceAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = TablesMissingMessage }); }
        }

        // A database that has not had Scripts/HikvisionAttendance.sql run on it
        // yet (SQL error 208, invalid object name) - say so plainly.
        private const string TablesMissingMessage =
            "The attendance machine tables are not in this database yet. Run Scripts/HikvisionAttendance.sql (or the deploy runner) on it.";

        private static bool TablesMissing(Exception? ex)
        {
            for (; ex != null; ex = ex.InnerException)
                if (ex is SqlException { Number: 208 or 207 }) return true;
            return false;
        }

        [HttpDelete("devices/{id:int}")]
        public async Task<ActionResult> Delete(int id) => await _service.DeleteDeviceAsync(id) ? NoContent() : NotFound();

        [HttpPost("devices/{id:int}/test")]
        public async Task<ActionResult<DeviceTestResultDto>> Test(int id)
        {
            try { return Ok(await _service.TestDeviceAsync(id)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        /// <param name="from">Read punches from this time instead of from the last one read - to fetch a missed day.</param>
        [HttpPost("devices/{id:int}/pull")]
        public async Task<ActionResult<DevicePullResultDto>> Pull(int id, [FromQuery] DateTime? from)
        {
            try { return Ok(await _service.PullAsync(id, from)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        // ── The machine's push ─────────────────────────────────────────

        /// <summary>
        /// Where the machine's HTTP Listening / event upload points. Open to
        /// the machine (it cannot log in); the key in the address is what
        /// lets a punch in, and an unknown key is refused.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("event/{key}")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult> Event(string key)
        {
            string body;
            if (Request.HasFormContentType)
            {
                // The machine posts multipart/form-data (event_log + picture);
                // ASP.NET has already read that into the form, so take the
                // JSON from its fields and any JSON file parts.
                var form = await Request.ReadFormAsync();
                var parts = form.SelectMany(f => f.Value.Select(v => v ?? ""))
                    .Where(v => v.TrimStart().StartsWith("{")).ToList();
                foreach (var file in form.Files.Where(f => (f.ContentType ?? "").Contains("json")))
                {
                    using var r = new StreamReader(file.OpenReadStream());
                    parts.Add(await r.ReadToEndAsync());
                }
                body = string.Join("\n", parts);
            }
            else
            {
                using var reader = new StreamReader(Request.Body);
                body = await reader.ReadToEndAsync();
            }

            return await _service.ReceivePushAsync(key, body, Request.ContentType) ? Ok() : NotFound();
        }

        // ── People ─────────────────────────────────────────────────────

        /// <param name="type">1 staff, 2 students.</param>
        [HttpGet("devices/{id:int}/people")]
        public async Task<ActionResult<List<DevicePersonRowDto>>> People(int id, [FromQuery] byte type = 1,
            [FromQuery] int? classId = null, [FromQuery] int? sectionId = null)
        {
            try { return Ok(await _service.GetPeopleAsync(id, type, classId, sectionId)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = BiometricTablesMissingMessage }); }
        }

        [HttpPost("devices/{id:int}/refresh")]
        public async Task<ActionResult<DeviceSyncResultDto>> Refresh(int id)
        {
            try { return Ok(await _service.RefreshFromDeviceAsync(id)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = BiometricTablesMissingMessage }); }
        }

        [HttpPost("devices/{id:int}/sync")]
        public async Task<ActionResult<DeviceSyncResultDto>> Sync(int id, DeviceSyncRequestDto dto)
        {
            try { return Ok(await _service.SyncPeopleAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = BiometricTablesMissingMessage }); }
        }

        /// <summary>Reads faces and fingerprints from the machine into EasySchool. No people given: everyone on it.</summary>
        [HttpPost("devices/{id:int}/fetch-biometrics")]
        public async Task<ActionResult<DeviceSyncResultDto>> FetchBiometrics(int id, DeviceSyncRequestDto dto)
        {
            var userId = Request.GetUserId();
            try { return Ok(await _service.FetchBiometricsAsync(id, dto, userId == 0 ? null : userId)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = BiometricTablesMissingMessage }); }
        }

        /// <summary>Reads faces and fingerprints from one machine and puts the people, with them, on another.</summary>
        [HttpPost("devices/{id:int}/copy-to/{targetId:int}")]
        public async Task<ActionResult<DeviceSyncResultDto>> CopyTo(int id, int targetId, DeviceSyncRequestDto dto)
        {
            var userId = Request.GetUserId();
            try { return Ok(await _service.CopyToDeviceAsync(id, targetId, dto, userId == 0 ? null : userId)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) when (TablesMissing(ex)) { return StatusCode(500, new { message = BiometricTablesMissingMessage }); }
        }

        private const string BiometricTablesMissingMessage =
            "The fingerprint table is not in the database yet. Run Scripts/HikvisionBiometrics.sql on the database, then try again.";

        [HttpPost("devices/{id:int}/remove")]
        public async Task<ActionResult<DeviceSyncResultDto>> Remove(int id, DeviceSyncRequestDto dto)
        {
            try { return Ok(await _service.RemovePeopleAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPut("devices/{id:int}/card")]
        public async Task<ActionResult<DeviceSyncResultDto>> Card(int id, SaveDeviceCardDto dto)
        {
            try { return Ok(await _service.SaveCardAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("face")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<DeviceSyncResultDto>> Face(SaveFacePhotoDto dto)
        {
            var userId = Request.GetUserId();
            try { return Ok(await _service.SaveFaceAsync(dto, userId == 0 ? null : userId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("face/{type}/{id:int}")]
        public async Task<ActionResult> GetFace(byte type, int id)
        {
            var face = await _service.GetFaceAsync(type, id);
            return face == null ? NotFound() : File(face, "image/jpeg");
        }

        // ── Log ────────────────────────────────────────────────────────

        [HttpGet("events")]
        public async Task<ActionResult<List<DeviceEventRowDto>>> Events([FromQuery] int? deviceId, [FromQuery] DateTime? date, [FromQuery] int take = 200) =>
            Ok(await _service.GetEventsAsync(deviceId, date, take));
    }
}
