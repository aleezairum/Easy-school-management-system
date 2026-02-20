using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class ChallanVoucherService : IChallanVoucherService
    {
        private readonly IChallanVoucherRepository _repository;

        public ChallanVoucherService(IChallanVoucherRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ChallanVoucherDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetByStudentAsync(int studentId)
        {
            return await _repository.GetByStudentAsync(studentId);
        }

        public async Task<IEnumerable<ChallanVoucherListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId)
        {
            return await _repository.GetByClassSectionAsync(classId, sectionId, sessionId);
        }

        public async Task<IEnumerable<ChallanDropdownDto>> GetPendingByStudentAsync(int studentId)
        {
            return await _repository.GetPendingByStudentAsync(studentId);
        }

        public async Task<ChallanVoucherDto> CreateAsync(CreateChallanVoucherDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<ChallanVoucherDto?> UpdateAsync(int id, UpdateChallanVoucherDto dto)
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
    }
}
