using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

public class AcademicSessionYearService : IAcademicSessionYearService
{
    private readonly IAcademicSessionYearRepository _repo;

    public AcademicSessionYearService(IAcademicSessionYearRepository repo)
    {
        _repo = repo;
    }

    public Task<ResponseDto> SaveAsync(
        AcademicSessionYearSaveDto dto,
        int userId,
        string userIp)
        => _repo.SaveAsync(dto, userId, userIp);

    public Task<List<AcademicSessionYear>> GetAllAsync()
        => _repo.GetAllAsync();

    public Task<AcademicSessionYear?> GetByIdAsync(int id)
        => _repo.GetByIdAsync(id);
}
