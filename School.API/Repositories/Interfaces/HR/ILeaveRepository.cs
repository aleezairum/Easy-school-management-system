using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface ILeaveRepository
    {
        Task<List<LeaveDto>> GetAllAsync(byte? status);
        Task<HrLeave> ApplyAsync(ApplyLeaveDto dto);
        Task<HrLeave?> UpdateStatusAsync(int id, byte status, int? approvedBy);
    }
}
