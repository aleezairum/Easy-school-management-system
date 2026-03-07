// Services/Implementations/Academic/ApplyFeeService.cs
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Academic
{
    public class ApplyFeeService : IApplyFeeService
    {
        private readonly IApplyFeeRepository _repo;

        public ApplyFeeService(IApplyFeeRepository repo)
        {
            _repo = repo;
        }

        public Task<List<StudentFeePreviewResultDto>> GetPreviewAsync(ApplyFeePreviewRequestDto dto)
            => _repo.GetPreviewAsync(dto);

        public async Task<ApplyFeeResponseDto> ApplyFeeAsync(ApplyFeeRequestDto dto, int userId, string userIp)
        {
            var response = new ApplyFeeResponseDto();

            var months = GetMonthRange(dto.MonthFrom, dto.MonthTo);

            foreach (var student in dto.StudentFees)
            {
                foreach (var month in months)
                {
                    response.TotalRequested++;

                    var saveDto = new SaveStudentFeeRequestDto
                    {
                        VID = 0,
                        VDate = new DateTime(month.Year, month.Month, 1),
                        StudentID = student.StudentId,
                        SessionID = student.SessionID,
                        SectionID = student.SectionID,
                        GradeID = student.GradeID,
                        FeeTypeID = dto.FeeTypeID,
                        FeeAmount = student.Amount,
                        ClearedAmount = 0,
                        Remarks = dto.Description,
                        IsClear = false
                    };

                    var result = await _repo.SaveStudentFeeAsync(saveDto, userId, userIp);

                    response.Results.Add(new ApplyFeeRowResultDto
                    {
                        StudentId = student.StudentId,
                        Month = month.ToString("yyyy-MM"),
                        VID = result?.VID ?? 0,
                        ReturnCode = result?.ReturnCode ?? 500,
                        ReturnMessage = result?.ReturnMessage ?? "No response"
                    });

                    if (result?.ReturnCode == 200) response.TotalSaved++;
                    else if (result?.ReturnCode == 409) response.TotalSkipped++;
                    else response.TotalFailed++;
                }
            }

            return response;
        }

        private static List<DateOnly> GetMonthRange(string from, string to)
        {
            var months = new List<DateOnly>();
            var start = DateOnly.ParseExact(from, "yyyy-MM");
            var end = DateOnly.ParseExact(to, "yyyy-MM");
            var current = start;

            while (current <= end)
            {
                months.Add(current);
                current = current.AddMonths(1);
            }

            return months;
        }
        public Task<List<StudentFeeListDto>> GetListAsync(GetStudentFeeRequestDto dto)
            => _repo.GetListAsync(dto);
        public async Task<ResponseDto> DeleteAsync(int vid, int userId, string userIp)
            => await _repo.DeleteAsync(vid, userId, userIp);

    }
}