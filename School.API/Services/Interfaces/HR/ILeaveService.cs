using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface ILeaveService
    {
        Task<List<LeaveDto>> GetAllAsync(byte? status);
        Task<LeaveDto> ApplyAsync(ApplyLeaveDto dto);
        Task<bool> SetStatusAsync(int id, byte status, int? approvedBy);
    }
}
