using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class SMSExamResultRepository : ISMSExamResultRepository
    {
        private readonly SchoolDbContext _context;

        public SMSExamResultRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSExamResultSaveDto>> GetAllAsync()
        {
            return await _context
                .Set<SMSExamResultSaveDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSExamResult @VID={0}, @ExamID={1}, @SubjectID={2}, @ClassID={3}, @SectionID={4}",
                    0, 0, 0, 0, 0)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SMSExamResultSaveDto?> GetByIdAsync(int vid)
        {
            var list = await _context
                .Set<SMSExamResultSaveDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSExamResult @VID={0}, @ExamID={1}, @SubjectID={2}, @ClassID={3}, @SectionID={4}",
                    vid, 0, 0, 0, 0)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<List<SMSExamResultSaveDto>> GetByExamAsync(int examId, int subjectId = 0, int classId = 0, int sectionId = 0)
        {
            return await _context
                .Set<SMSExamResultSaveDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSExamResult @VID={0}, @ExamID={1}, @SubjectID={2}, @ClassID={3}, @SectionID={4}",
                    0, examId, subjectId, classId, sectionId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<SMSExamResultSaveDto>> FillComboAsync(int examId, int studentId, int subjectId = 0, int classId = 0, int sectionId = 0)
        {
            return await _context
                .Set<SMSExamResultSaveDto>()
                .FromSqlRaw(
                    "EXEC SpFillCombo_SMSExamResult @ExamID={0}, @StudentID={1}, @SubjectID={2}, @ClassID={3}, @SectionID={4}",
                    examId, studentId, subjectId, classId, sectionId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ExamResultSheetDto> GetResultSheetAsync(int examId, int classId, int sectionId, int prevExamId = 0)
        {
            // The SP returns two result sets and a dynamic set of subject columns,
            // so we read it with ADO.NET rather than mapping to a fixed entity.
            var result = new ExamResultSheetDto();

            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.SpRPT_SMSExamResultSheet";
                AddParam(cmd, "@ExamID", examId);
                AddParam(cmd, "@ClassID", classId);
                AddParam(cmd, "@SectionID", sectionId);
                AddParam(cmd, "@PrevExamID", prevExamId);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: subject header (column definitions)
                while (await reader.ReadAsync())
                {
                    result.Subjects.Add(new ResultSheetSubjectDto
                    {
                        SubjectId   = GetInt(reader, "SubjectID"),
                        SubjectName = GetString(reader, "SubjectName"),
                        TotalMarks  = GetDecimal(reader, "TotalMarks"),
                        PassMarks   = GetDecimal(reader, "PassMarks")
                    });
                }

                // Result set 2: student rows (fixed columns + one column per subject)
                var fixedCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "RollNo", "StudentName", "FatherName", "Position", "CurrentPct", "PrevPct", "Result", "Total"
                };

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var student = new ResultSheetStudentDto
                        {
                            RollNo      = GetString(reader, "RollNo"),
                            StudentName = GetString(reader, "StudentName"),
                            FatherName  = GetString(reader, "FatherName"),
                            Position    = GetNullableInt(reader, "Position"),
                            CurrentPct  = GetDecimal(reader, "CurrentPct"),
                            PrevPct     = GetDecimal(reader, "PrevPct"),
                            Result      = GetString(reader, "Result"),
                            Total       = GetDecimal(reader, "Total")
                        };

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var col = reader.GetName(i);
                            if (fixedCols.Contains(col)) continue;   // subject column

                            student.Marks[col] = reader.IsDBNull(i)
                                ? (decimal?)null
                                : Convert.ToDecimal(reader.GetValue(i));
                        }

                        result.Students.Add(student);
                    }
                }
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<ExamResultCardDto> GetResultCardAsync(int examId, int classId, int sectionId, int prevExamId = 0, int studentId = 0)
        {
            // Like the result sheet, the SP returns two result sets:
            //   (1) one header row per student
            //   (2) one detail row per student-subject
            var result = new ExamResultCardDto();

            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.SpRPT_SMSExamResultCard";
                AddParam(cmd, "@ExamID", examId);
                AddParam(cmd, "@ClassID", classId);
                AddParam(cmd, "@SectionID", sectionId);
                AddParam(cmd, "@PrevExamID", prevExamId);
                AddParam(cmd, "@StudentID", studentId);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: student headers
                var byId = new Dictionary<int, ResultCardStudentDto>();
                while (await reader.ReadAsync())
                {
                    var student = new ResultCardStudentDto
                    {
                        StudentId     = GetInt(reader, "StudentID"),
                        RollNo        = GetString(reader, "RollNo"),
                        StudentName   = GetString(reader, "StudentName"),
                        FatherName    = GetString(reader, "FatherName"),
                        ClassName     = GetString(reader, "ClassName"),
                        SectionName   = GetString(reader, "SectionName"),
                        Position      = GetNullableInt(reader, "Position"),
                        TotalStudents = GetInt(reader, "TotalStudents"),
                        HighestMarks  = GetDecimal(reader, "HighestMarks"),
                        CurrentPct    = GetDecimal(reader, "CurrentPct"),
                        PrevPct       = GetDecimal(reader, "PrevPct"),
                        GrandTotal    = GetDecimal(reader, "GrandTotal"),
                        GrandPass     = GetDecimal(reader, "GrandPass"),
                        GrandObt      = GetDecimal(reader, "GrandObt"),
                        GrandStatus   = GetString(reader, "GrandStatus"),
                        PresentDays   = GetInt(reader, "PresentDays"),
                        SchoolDays    = GetInt(reader, "SchoolDays"),
                        AttendancePct = GetDecimal(reader, "AttendancePct")
                    };
                    result.Students.Add(student);
                    byId[student.StudentId] = student;
                }

                // Result set 2: subject detail rows, attached to their student
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var sid = GetInt(reader, "StudentID");
                        if (!byId.TryGetValue(sid, out var student)) continue;

                        student.Subjects.Add(new ResultCardSubjectDto
                        {
                            SubjectName   = GetString(reader, "SubjectName"),
                            VDate         = GetNullableDateTime(reader, "VDate"),
                            TotalMarks    = GetDecimal(reader, "TotalMarks"),
                            PassMarks     = GetDecimal(reader, "PassMarks"),
                            MarksObtained = GetDecimal(reader, "MarksObtained"),
                            Status        = GetString(reader, "Status")
                        });
                    }
                }
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId)
        {
            var result = new ExamAnnouncementDto();

            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.SpRPT_SMSExamResultAnnouncement";
                AddParam(cmd, "@ExamID", examId);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: overall (single row)
                if (await reader.ReadAsync())
                    result.Overall = ReadStatRow(reader);

                // Result set 2: campus wise
                if (await reader.NextResultAsync())
                    while (await reader.ReadAsync())
                        result.Campuses.Add(ReadStatRow(reader));

                // Result set 3: class wise
                if (await reader.NextResultAsync())
                    while (await reader.ReadAsync())
                        result.Classes.Add(ReadStatRow(reader));

                // Result set 4: top positions per class
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.TopPositions.Add(new AnnouncementTopRowDto
                        {
                            ClassName     = GetString(reader, "ClassName"),
                            Position      = GetNullableInt(reader, "Position"),
                            RollNo        = GetString(reader, "RollNo"),
                            StudentName   = GetString(reader, "StudentName"),
                            FatherName    = GetString(reader, "FatherName"),
                            SectionName   = GetString(reader, "SectionName"),
                            TotalMarks    = GetDecimal(reader, "TotalMarks"),
                            ObtainedMarks = GetDecimal(reader, "ObtainedMarks"),
                            Percent       = GetDecimal(reader, "Percent")
                        });
                    }
                }
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }

            return result;
        }

        private static ResultStatRowDto ReadStatRow(DbDataReader reader) => new()
        {
            Name          = HasColumn(reader, "Name") ? GetString(reader, "Name") : null,
            TotalStudents = GetInt(reader, "TotalStudents"),
            TotalPass     = GetInt(reader, "TotalPass"),
            TotalFail     = GetInt(reader, "TotalFail"),
            PassPct       = GetDecimal(reader, "PassPct"),
            FailPct       = GetDecimal(reader, "FailPct")
        };

        public Task<List<ExamPositionRowDto>> GetPositionReportAsync(int examId, int classId)
            => ReadStudentRankRowsAsync("dbo.SpRPT_SMSExamStudentPosition", examId, classId);

        public Task<List<ExamPositionRowDto>> GetPercentageReportAsync(int examId, int classId)
            => ReadStudentRankRowsAsync("dbo.SpRPT_SMSExamStudentPercentage", examId, classId);

        public async Task<ExamExecutiveDto> GetExecutiveReportAsync(int examId, int classId, int sectionId)
        {
            var result = new ExamExecutiveDto();

            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.SpRPT_SMSExamExecutive";
                AddParam(cmd, "@ExamID", examId);
                AddParam(cmd, "@ClassID", classId);
                AddParam(cmd, "@SectionID", sectionId);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: overall summary (single row)
                if (await reader.ReadAsync())
                {
                    result.Overall = new ExecutiveOverallDto
                    {
                        TotalStudents = GetInt(reader, "TotalStudents"),
                        TotalPass     = GetInt(reader, "TotalPass"),
                        TotalFail     = GetInt(reader, "TotalFail"),
                        ResultPct     = GetDecimal(reader, "ResultPct")
                    };
                }

                // Result set 2: subject wise status + teacher percentage
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Subjects.Add(new ExecutiveSubjectDto
                        {
                            SubjectName = GetString(reader, "SubjectName"),
                            FailCount   = GetInt(reader, "FailCount"),
                            FailPct     = GetDecimal(reader, "FailPct"),
                            PassCount   = GetInt(reader, "PassCount"),
                            PassPct     = GetDecimal(reader, "PassPct"),
                            TeacherName = GetString(reader, "TeacherName"),
                            Obtained    = GetDecimal(reader, "Obtained"),
                            Total       = GetDecimal(reader, "Total"),
                            Percent     = GetDecimal(reader, "Percent")
                        });
                    }
                }
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }

            return result;
        }

        // Shared reader for the Position / Percentage reports — both stored
        // procedures return the identical column set (one row per student).
        private async Task<List<ExamPositionRowDto>> ReadStudentRankRowsAsync(string spName, int examId, int classId)
        {
            var rows = new List<ExamPositionRowDto>();

            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = spName;
                AddParam(cmd, "@ExamID", examId);
                AddParam(cmd, "@ClassID", classId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new ExamPositionRowDto
                    {
                        Sr            = GetInt(reader, "Sr"),
                        Position      = GetNullableInt(reader, "Position"),
                        RollNo        = GetString(reader, "RollNo"),
                        StudentName   = GetString(reader, "StudentName"),
                        FatherName    = GetString(reader, "FatherName"),
                        SectionName   = GetString(reader, "SectionName"),
                        TotalMarks    = GetDecimal(reader, "TotalMarks"),
                        ObtainedMarks = GetDecimal(reader, "ObtainedMarks"),
                        Percent       = GetDecimal(reader, "Percent"),
                        Status        = GetString(reader, "Status")
                    });
                }
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }

            return rows;
        }

        private static DateTime? GetNullableDateTime(DbDataReader reader, string name)
        {
            if (!HasColumn(reader, name)) return null;
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(i));
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private static bool HasColumn(DbDataReader reader, string name)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string? GetString(DbDataReader reader, string name)
        {
            if (!HasColumn(reader, name)) return null;
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
        }

        private static int GetInt(DbDataReader reader, string name)
        {
            if (!HasColumn(reader, name)) return 0;
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i));
        }

        private static int? GetNullableInt(DbDataReader reader, string name)
        {
            if (!HasColumn(reader, name)) return null;
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? (int?)null : Convert.ToInt32(reader.GetValue(i));
        }

        private static decimal? GetDecimal(DbDataReader reader, string name)
        {
            if (!HasColumn(reader, name)) return null;
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(i));
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSExamResult @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(SMSExamResultSaveDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSExamResult " +
                    "@VID={0}, @ExamID={1}, @ClassID={2}, @SectionID={3}, @SubjectID={4}, @StudentID={5}, " +
                    "@VDate={6}, @TotalMarks={7}, @PassMarks={8}, @MarksObtained={9}, " +
                    "@Grade={10}, @IsAbsent={11}, @IsPassed={12}, @Remarks={13}, @IsActive={14}, " +
                    "@UserID={15}, @UserIP={16}",
                    dto.VID,
                    dto.ExamID,
                    dto.ClassID,
                    dto.SectionID,
                    dto.SubjectID,
                    dto.StudentID,
                    (object?)dto.VDate?.Date ?? DBNull.Value,
                    (object?)dto.TotalMarks ?? (object)100m,
                    (object?)dto.PassMarks ?? DBNull.Value,
                    (object?)dto.MarksObtained ?? DBNull.Value,
                    (object?)dto.Grade ?? DBNull.Value,
                    dto.IsAbsent,
                    dto.IsPassed,
                    (object?)dto.Remarks ?? DBNull.Value,
                    dto.IsActive ?? true,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }
    }
}
