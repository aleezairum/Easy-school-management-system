using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public SessionController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/session
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions()
        //{
        //    var sessions = await _context.Sessions
        //        .OrderByDescending(s => s.StartDate)
        //        .Select(s => new SessionDto
        //        {
        //            Id = s.Id,
        //            SessionName = s.SessionName,
        //            StartDate = s.StartDate,
        //            EndDate = s.EndDate,
        //            IsCurrent = s.IsCurrent,
        //            IsActive = s.IsActive
        //        })
        //        .ToListAsync();

        //    return Ok(sessions);
        //}

        //// GET: api/session/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<SessionDto>> GetSession(int id)
        //{
        //    var session = await _context.Sessions.FindAsync(id);

        //    if (session == null)
        //    {
        //        return NotFound(new { message = "Session not found" });
        //    }

        //    return Ok(new SessionDto
        //    {
        //        Id = session.Id,
        //        SessionName = session.SessionName,
        //        StartDate = session.StartDate,
        //        EndDate = session.EndDate,
        //        IsCurrent = session.IsCurrent,
        //        IsActive = session.IsActive
        //    });
        //}

        //// GET: api/session/current
        //[HttpGet("current")]
        //public async Task<ActionResult<SessionDto>> GetCurrentSession()
        //{
        //    var session = await _context.Sessions
        //        .Where(s => s.IsCurrent && s.IsActive)
        //        .FirstOrDefaultAsync();

        //    if (session == null)
        //    {
        //        return NotFound(new { message = "No current session found" });
        //    }

        //    return Ok(new SessionDto
        //    {
        //        Id = session.Id,
        //        SessionName = session.SessionName,
        //        StartDate = session.StartDate,
        //        EndDate = session.EndDate,
        //        IsCurrent = session.IsCurrent,
        //        IsActive = session.IsActive
        //    });
        //}

        //// POST: api/session
        //[HttpPost]
        //public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionDto dto)
        //{
        //    // Check if session name already exists
        //    var existingSession = await _context.Sessions
        //        .FirstOrDefaultAsync(s => s.SessionName == dto.SessionName);

        //    if (existingSession != null)
        //    {
        //        return BadRequest(new { message = "A session with this name already exists" });
        //    }

        //    // Validate dates
        //    if (dto.EndDate <= dto.StartDate)
        //    {
        //        return BadRequest(new { message = "End date must be after start date" });
        //    }

        //    // If this is set as current, unset other current sessions
        //    if (dto.IsCurrent)
        //    {
        //        var currentSessions = await _context.Sessions
        //            .Where(s => s.IsCurrent)
        //            .ToListAsync();

        //        foreach (var s in currentSessions)
        //        {
        //            s.IsCurrent = false;
        //        }
        //    }

        //    var session = new Session
        //    {
        //        SessionName = dto.SessionName,
        //        StartDate = dto.StartDate,
        //        EndDate = dto.EndDate,
        //        IsCurrent = dto.IsCurrent,
        //        IsActive = dto.IsActive,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _context.Sessions.Add(session);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetSession), new { id = session.Id }, new SessionDto
        //    {
        //        Id = session.Id,
        //        SessionName = session.SessionName,
        //        StartDate = session.StartDate,
        //        EndDate = session.EndDate,
        //        IsCurrent = session.IsCurrent,
        //        IsActive = session.IsActive
        //    });
        //}

        //// PUT: api/session/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateSession(int id, UpdateSessionDto dto)
        //{
        //    if (id != dto.Id)
        //    {
        //        return BadRequest(new { message = "ID mismatch" });
        //    }

        //    var session = await _context.Sessions.FindAsync(id);
        //    if (session == null)
        //    {
        //        return NotFound(new { message = "Session not found" });
        //    }

        //    // Check if session name already exists (excluding current session)
        //    var existingSession = await _context.Sessions
        //        .FirstOrDefaultAsync(s => s.SessionName == dto.SessionName && s.Id != id);

        //    if (existingSession != null)
        //    {
        //        return BadRequest(new { message = "A session with this name already exists" });
        //    }

        //    // Validate dates
        //    if (dto.EndDate <= dto.StartDate)
        //    {
        //        return BadRequest(new { message = "End date must be after start date" });
        //    }

        //    // If this is set as current, unset other current sessions
        //    if (dto.IsCurrent && !session.IsCurrent)
        //    {
        //        var currentSessions = await _context.Sessions
        //            .Where(s => s.IsCurrent && s.Id != id)
        //            .ToListAsync();

        //        foreach (var s in currentSessions)
        //        {
        //            s.IsCurrent = false;
        //        }
        //    }

        //    session.SessionName = dto.SessionName;
        //    session.StartDate = dto.StartDate;
        //    session.EndDate = dto.EndDate;
        //    session.IsCurrent = dto.IsCurrent;
        //    session.IsActive = dto.IsActive;
        //    session.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(new SessionDto
        //    {
        //        Id = session.Id,
        //        SessionName = session.SessionName,
        //        StartDate = session.StartDate,
        //        EndDate = session.EndDate,
        //        IsCurrent = session.IsCurrent,
        //        IsActive = session.IsActive
        //    });
        //}

        //// DELETE: api/session/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteSession(int id)
        //{
        //    var session = await _context.Sessions.FindAsync(id);
        //    if (session == null)
        //    {
        //        return NotFound(new { message = "Session not found" });
        //    }

        //    if (session.IsCurrent)
        //    {
        //        return BadRequest(new { message = "Cannot delete the current active session" });
        //    }

        //    _context.Sessions.Remove(session);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Session deleted successfully" });
        //}

        //// POST: api/session/5/setcurrent
        //[HttpPost("{id}/setcurrent")]
        //public async Task<IActionResult> SetCurrentSession(int id)
        //{
        //    var session = await _context.Sessions.FindAsync(id);
        //    if (session == null)
        //    {
        //        return NotFound(new { message = "Session not found" });
        //    }

        //    if (!session.IsActive)
        //    {
        //        return BadRequest(new { message = "Cannot set an inactive session as current" });
        //    }

        //    // Unset all current sessions
        //    var currentSessions = await _context.Sessions
        //        .Where(s => s.IsCurrent)
        //        .ToListAsync();

        //    foreach (var s in currentSessions)
        //    {
        //        s.IsCurrent = false;
        //    }

        //    // Set this session as current
        //    session.IsCurrent = true;
        //    session.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Session set as current successfully" });
        //}
    }
}
