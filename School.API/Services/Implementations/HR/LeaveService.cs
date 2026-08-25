using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class LeaveService : ILeaveService
    {
        private readonly ILeaveRepository _repository;

        public LeaveService(ILeaveRepository repository)
        {
            _repository = repository;
        }

        public Task<List<LeaveDto>> GetAllAsync(byte? status) => _repository.GetAllAsync(status);

        public async Task<LeaveDto> ApplyAsync(ApplyLeaveDto dto)
        {
            var entity = await _repository.ApplyAsync(dto);
            var all = await _repository.GetAllAsync(null);
            return all.First(l => l.Id == entity.Id);
        }

        public async Task<bool> SetStatusAsync(int id, byte status, int? approvedBy)
        {
            var entity = await _repository.UpdateStatusAsync(id, status, approvedBy);
            return entity != null;
        }
    }
}
