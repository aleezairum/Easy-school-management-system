using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class SalaryService : ISalaryService
    {
        private readonly ISalaryRepository _repository;

        public SalaryService(ISalaryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SalaryListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<SalaryDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<SalaryListDto>> GetByMonthYearAsync(int month, int year)
        {
            return await _repository.GetByMonthYearAsync(month, year);
        }

        public async Task<IEnumerable<SalaryListDto>> GetByEmployeeAsync(int employeeId)
        {
            return await _repository.GetByEmployeeAsync(employeeId);
        }

        public async Task<SalaryDto> CreateAsync(CreateSalaryDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<SalaryDto?> UpdateAsync(int id, UpdateSalaryDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null)
                return null;

            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task ApproveSalariesAsync(List<int> salaryIds)
        {
            await _repository.ApproveSalariesAsync(salaryIds);
        }

        public async Task PaySalaryAsync(PaySalaryDto dto)
        {
            await _repository.PaySalaryAsync(dto);
        }
    }
}
