using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class EmployeePayService : IEmployeePayService
    {
        private readonly IEmployeePayRepository _repository;

        public EmployeePayService(IEmployeePayRepository repository)
        {
            _repository = repository;
        }

        public Task<List<EmployeePayDto>> GetHistoryAsync(int employeeId) => _repository.GetHistoryAsync(employeeId);

        public async Task<EmployeePayDto> CreateAsync(SaveEmployeePayDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var history = await _repository.GetHistoryAsync(dto.EmployeeId);
            return history.First(h => h.Id == entity.Id);
        }
    }
}
