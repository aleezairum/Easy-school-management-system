using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public Task<List<EmployeeListDto>> GetAllAsync() => _repository.GetAllAsync();
        public Task<EmployeeDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task<List<EmployeeDropdownDto>> GetDropdownAsync() => _repository.GetDropdownAsync();

        public async Task<EmployeeDto> CreateAsync(SaveEmployeeDto dto)
        {
            if (await _repository.CodeExistsAsync(dto.EmployeeCode))
                throw new InvalidOperationException("Employee code already exists.");

            var entity = await _repository.CreateAsync(dto);
            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<EmployeeDto?> UpdateAsync(int id, SaveEmployeeDto dto)
        {
            if (await _repository.CodeExistsAsync(dto.EmployeeCode, id))
                throw new InvalidOperationException("Employee code already exists.");

            var entity = await _repository.UpdateAsync(id, dto);
            return entity == null ? null : await _repository.GetByIdAsync(id);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
    }
}
