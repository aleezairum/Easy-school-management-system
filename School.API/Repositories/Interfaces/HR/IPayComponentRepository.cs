using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IPayComponentRepository
    {
        Task<List<PayComponentDto>> GetAllAsync();
        Task<PayComponentDto?> GetByIdAsync(int id);
        Task<HrPayComponent?> GetEntityByIdAsync(int id);
        Task<HrPayComponent> CreateAsync(SavePayComponentDto dto);
        Task<HrPayComponent?> UpdateAsync(int id, SavePayComponentDto dto);
        Task<bool> DeleteAsync(int id);

        /// <summary>Resolved, most-specific-wins components applicable to an employee for a pay period (section 4.1).</summary>
        Task<List<HrPayComponent>> GetApplicableAsync(DateTime periodStart, DateTime periodEnd, int? designationId, int? hrGradeId, int employeeId);
    }
}
