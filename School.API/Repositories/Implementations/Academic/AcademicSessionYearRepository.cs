using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

public class AcademicSessionYearRepository : IAcademicSessionYearRepository
{
    private readonly SchoolDbContext _context;

    public AcademicSessionYearRepository(SchoolDbContext context)
    {
        _context = context;
    }

    public Task<List<AcademicSessionYear>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<AcademicSessionYear?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<ResponseDto> SaveAsync(
        AcademicSessionYearSaveDto dto,
        int userId,
        string userIp)
    {
        var result = await _context
            .Set<ResponseDto>()
            .FromSqlRaw(
                "EXEC SpSave_AcademicSessionYear @VID={0}, @VName={1}, @DateFrom={2}, @DateTo={3}, @IsActive={4}, @UserID={5}, @UserIP={6}",
                dto.VID,
                dto.VName,
                dto.DateFrom,
                dto.DateTo,
                dto.IsActive,
                userId,
                userIp)
            .AsNoTracking()
            .FirstAsync();

        return result;
    }

    //public async Task<List<AcademicSessionYear>> GetAllAsync()
    //{
    //    return await _context.AcademicSessionYears
    //        .AsNoTracking()
    //        .ToListAsync();
    //}

    //public async Task<AcademicSessionYear?> GetByIdAsync(int id)
    //{
    //    return await _context.AcademicSessionYears
    //        .AsNoTracking()
    //        .FirstOrDefaultAsync(x => x.VID == id);
    //}

    Task<ResponseDto> IAcademicSessionYearRepository.SaveAsync(AcademicSessionYearSaveDto dto, int userId, string userIp)
    {
        throw new NotImplementedException();
    }
}
