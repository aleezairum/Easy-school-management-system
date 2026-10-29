/* ============================================================================
   Academy Subject Allocation (academy time table) + Academy Teacher Wise Result
   ----------------------------------------------------------------------------
   Run on the target database:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AcademyStaffSubject.sql

   The academy (after-school classes) has its own staff -> subject allocation,
   per exam, exactly like Subject Allocation (SMSStaffSubject) has for the
   school day. It is a SEPARATE table so the school allocation, the school
   time table and every existing teacher report stay exactly as they are.

   Table   : SMSAcademyStaffSubject
             SectionID NULL = the subject is taught to the academy students of
             EVERY section of the class (academy groups often mix sections).
   SPs     : SpGet_SMSAcademyStaffSubject     (list)
             SpSave_SMSAcademyStaffSubject    (insert / update one row)
             SpDelete_SMSAcademyStaffSubject  (delete one row)
             SpCopy_SMSAcademyStaffSubject    (copy one exam's allocation into another)
             SpRPT_SMSExamAcademyTeacherWise  (exam result of each academy teacher)

   Academy students are the ones with "Avail Academy" on
   (SMSStudentAcademic.IsAvailAcademy = 1) - the same students the academy
   attendance screen lists. An academy teacher's result for a subject is the
   exam result of THOSE students only, in the class (and section) allocated.
   The percentage is the same "Average %" the school Teacher Wise report uses:
   (pass % by pass marks + obtained %) / 2.

   New objects only. Nothing existing is changed. Safe to run more than once.
   Target: SQL Server 2019
   ============================================================================ */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- Table ---- */
IF OBJECT_ID('dbo.SMSAcademyStaffSubject', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSAcademyStaffSubject
    (
        VID          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSAcademyStaffSubject PRIMARY KEY,
        ExamID       INT NOT NULL,
        TeacherID    INT NOT NULL,
        ClassID      INT NOT NULL,
        SectionID    INT NULL,                 -- NULL = all sections of the class
        SubjectID    INT NOT NULL,
        LectureNo    INT NULL,
        IsActive     BIT NOT NULL CONSTRAINT DF_SMSAcademyStaffSubject_IsActive DEFAULT (1),
        InsertedBy   INT NULL,
        InsertedDate DATETIME NULL,
        InsertedIp   VARCHAR(200) NULL,
        UpdatedBy    INT NULL,
        UpdatedDate  DATETIME NULL,
        UpdatedIp    VARCHAR(200) NULL
    );

    CREATE INDEX IX_SMSAcademyStaffSubject_Exam_Teacher
        ON dbo.SMSAcademyStaffSubject (ExamID, TeacherID) INCLUDE (ClassID, SectionID, SubjectID, LectureNo, IsActive);
END
GO

/* ---- SpGet_SMSAcademyStaffSubject ---- */
IF OBJECT_ID('dbo.SpGet_SMSAcademyStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpGet_SMSAcademyStaffSubject;
GO
CREATE PROCEDURE dbo.SpGet_SMSAcademyStaffSubject
    @VID       INT = 0,
    @TeacherID INT = 0,
    @ClassID   INT = 0,
    @SectionID INT = 0,
    @ExamID    INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SS.VID                  AS Vid,
        SS.ExamID               AS ExamId,
        EX.VName                AS ExamName,
        SS.TeacherID            AS TeacherId,
        T.VName                 AS TeacherName,
        SS.ClassID              AS ClassId,
        C.VName                 AS ClassName,
        ISNULL(SS.SectionID, 0) AS SectionId,     -- 0 = all sections
        SE.VName                AS SectionName,
        SS.SubjectID            AS SubjectId,
        SU.VName                AS SubjectName,
        SS.LectureNo            AS LectureNo,
        SS.IsActive             AS IsActive
    FROM        dbo.SMSAcademyStaffSubject SS
    LEFT JOIN   dbo.SMSExam    EX ON EX.VID = SS.ExamID
    LEFT JOIN   dbo.Employees  T  ON T.Id   = SS.TeacherID
    LEFT JOIN   dbo.SMSClass   C  ON C.VID  = SS.ClassID
    LEFT JOIN   dbo.SMSSection SE ON SE.VID = SS.SectionID
    LEFT JOIN   dbo.SMSSubject SU ON SU.VID = SS.SubjectID
    WHERE       (@VID = 0 OR SS.VID = @VID)
      AND       (@TeacherID = 0 OR SS.TeacherID = @TeacherID)
      AND       (@ClassID   = 0 OR SS.ClassID   = @ClassID)
      AND       (@SectionID = 0 OR SS.SectionID = @SectionID)
      AND       (@ExamID    = 0 OR SS.ExamID    = @ExamID)
      AND       SS.IsActive = 1
    ORDER BY    EX.VID DESC, T.VName, SS.ClassID, SS.SectionID, SS.LectureNo;
END
GO

/* ---- SpSave_SMSAcademyStaffSubject ---- */
IF OBJECT_ID('dbo.SpSave_SMSAcademyStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpSave_SMSAcademyStaffSubject;
GO
CREATE PROCEDURE dbo.SpSave_SMSAcademyStaffSubject
    @VID       INT = 0,
    @ExamID    INT,
    @TeacherID INT,
    @ClassID   INT,
    @SectionID INT = NULL,          -- NULL / 0 = all sections
    @SubjectID INT,
    @LectureNo INT = NULL,
    @IsActive  BIT = 1,
    @UserID    INT,
    @UserIP    VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @SectionID = 0 SET @SectionID = NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSExam WHERE VID = @ExamID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'Please select a valid exam.' AS ReturnMessage;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SMSAcademyStaffSubject
               WHERE ExamID    = @ExamID
                 AND TeacherID = @TeacherID
                 AND ClassID   = @ClassID
                 AND ISNULL(SectionID, 0) = ISNULL(@SectionID, 0)
                 AND SubjectID = @SubjectID
                 AND ISNULL(LectureNo, -1) = ISNULL(@LectureNo, -1)
                 AND VID <> @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'This subject is already allocated to that staff member for this exam.' AS ReturnMessage;
        RETURN;
    END

    IF (@VID = 0)
    BEGIN
        INSERT INTO dbo.SMSAcademyStaffSubject
            (ExamID, TeacherID, ClassID, SectionID, SubjectID, LectureNo, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES
            (@ExamID, @TeacherID, @ClassID, @SectionID, @SubjectID, @LectureNo, @IsActive, @UserID, GETDATE(), @UserIP);

        SELECT CAST(SCOPE_IDENTITY() AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode,
               'Record inserted successfully.' AS ReturnMessage;
    END
    ELSE
    BEGIN
        UPDATE dbo.SMSAcademyStaffSubject
        SET    ExamID = @ExamID, TeacherID = @TeacherID, ClassID = @ClassID, SectionID = @SectionID,
               SubjectID = @SubjectID, LectureNo = @LectureNo, IsActive = @IsActive,
               UpdatedBy = @UserID, UpdatedDate = GETDATE(), UpdatedIp = @UserIP
        WHERE  VID = @VID;

        SELECT CAST(@VID AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode,
               'Record updated successfully.' AS ReturnMessage;
    END
END
GO

/* ---- SpDelete_SMSAcademyStaffSubject ---- */
IF OBJECT_ID('dbo.SpDelete_SMSAcademyStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDelete_SMSAcademyStaffSubject;
GO
CREATE PROCEDURE dbo.SpDelete_SMSAcademyStaffSubject
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SMSAcademyStaffSubject WHERE VID = @VID;

    SELECT CAST(@VID AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode,
           'Record deleted successfully.' AS ReturnMessage;
END
GO

/* ---- SpCopy_SMSAcademyStaffSubject ----
   Additive: rows the target exam already has are skipped, so copying twice is a no-op. */
IF OBJECT_ID('dbo.SpCopy_SMSAcademyStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpCopy_SMSAcademyStaffSubject;
GO
CREATE PROCEDURE dbo.SpCopy_SMSAcademyStaffSubject
    @FromExamID INT,
    @ToExamID   INT,
    @UserID     INT,
    @UserIP     VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromExamID = @ToExamID
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'Source and target exam cannot be the same.' AS ReturnMessage;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSExam WHERE VID = @ToExamID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'The exam to copy into does not exist.' AS ReturnMessage;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSAcademyStaffSubject WHERE ExamID = @FromExamID AND IsActive = 1)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'That exam has no academy allocation to copy.' AS ReturnMessage;
        RETURN;
    END

    INSERT INTO dbo.SMSAcademyStaffSubject
        (ExamID, TeacherID, ClassID, SectionID, SubjectID, LectureNo, IsActive, InsertedBy, InsertedDate, InsertedIp)
    SELECT @ToExamID, s.TeacherID, s.ClassID, s.SectionID, s.SubjectID, s.LectureNo, 1, @UserID, GETDATE(), @UserIP
    FROM   dbo.SMSAcademyStaffSubject s
    WHERE  s.ExamID = @FromExamID
      AND  s.IsActive = 1
      AND  NOT EXISTS (SELECT 1 FROM dbo.SMSAcademyStaffSubject d
                       WHERE d.ExamID    = @ToExamID
                         AND d.TeacherID = s.TeacherID
                         AND d.ClassID   = s.ClassID
                         AND ISNULL(d.SectionID, 0) = ISNULL(s.SectionID, 0)
                         AND d.SubjectID = s.SubjectID
                         AND ISNULL(d.LectureNo, -1) = ISNULL(s.LectureNo, -1));

    DECLARE @Copied INT = @@ROWCOUNT;

    SELECT CAST(@Copied AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode,
           CASE WHEN @Copied = 0
                THEN 'Nothing to copy - the target exam already has these allocations.'
                ELSE CAST(@Copied AS VARCHAR(10)) + ' allocation(s) copied.'
           END AS ReturnMessage;
END
GO

/* ---- SpRPT_SMSExamAcademyTeacherWise ----
   Same columns as SpRPT_SMSExamTeacherWise, so the same report page shows it.
   One row per (academy teacher, subject, class, section) allocated in @ExamID
   that has results from academy students. SectionId 0 = all sections.
   Excluded students (SMSExamExclude) are left out, as in every exam report. */
IF OBJECT_ID('dbo.SpRPT_SMSExamAcademyTeacherWise', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamAcademyTeacherWise;
GO
CREATE PROCEDURE dbo.SpRPT_SMSExamAcademyTeacherWise
    @ExamID    INT,
    @TeacherID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TeacherID = 0 SET @TeacherID = NULL;

    ;WITH Assign AS
    (
        SELECT DISTINCT TeacherID, ClassID, SectionID, SubjectID
        FROM   dbo.SMSAcademyStaffSubject
        WHERE  ExamID   = @ExamID
          AND  IsActive = 1
          AND  (@TeacherID IS NULL OR TeacherID = @TeacherID)
    ),
    -- Results of academy students only.
    Res AS
    (
        SELECT R.ClassID, R.SectionID, R.SubjectID, R.IsAbsent, R.IsPassed, R.TotalMarks, R.MarksObtained
        FROM   dbo.SMSExamResult R
        WHERE  R.ExamID   = @ExamID
          AND  R.IsActive = 1
          AND  EXISTS (SELECT 1 FROM dbo.SMSStudentAcademic SA
                       WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1
                         AND ISNULL(SA.IsAvailAcademy, 0) = 1)
          AND  NOT EXISTS (SELECT 1 FROM dbo.SMSExamExclude X
                           WHERE X.ExamID = @ExamID AND X.StudentID = R.StudentID)
    ),
    Agg AS
    (
        SELECT
            A.TeacherID, A.ClassID, A.SectionID, A.SubjectID,
            SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END)                               AS Appeared,
            SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1 THEN 1 ELSE 0 END) AS Passed,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END)       AS TotalMarks,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END)       AS ObtainedMarks
        FROM        Assign A
        INNER JOIN  Res    R ON R.ClassID   = A.ClassID
                            AND R.SubjectID = A.SubjectID
                            AND (A.SectionID IS NULL OR R.SectionID = A.SectionID)
        GROUP BY    A.TeacherID, A.ClassID, A.SectionID, A.SubjectID
    )
    SELECT
        A.TeacherID                           AS TeacherId,
        T.VName                               AS TeacherName,
        A.SubjectID                           AS SubjectId,
        SU.VName                              AS SubjectName,
        A.ClassID                             AS ClassId,
        CL.VName                              AS ClassName,
        ISNULL(A.SectionID, 0)                AS SectionId,
        SE.VName                              AS SectionName,
        SU.VName
            + CASE WHEN CL.VName IS NULL THEN '' ELSE ' ' + CL.VName END
            + CASE WHEN SE.VName IS NULL THEN '' ELSE '-' + SE.VName END AS ColumnLabel,
        A.Appeared,
        CAST(A.TotalMarks    AS DECIMAL(9,2)) AS TotalMarks,
        CAST(A.ObtainedMarks AS DECIMAL(9,2)) AS ObtainedMarks,
        CAST(
            (
                CASE WHEN A.Appeared   > 0 THEN A.Passed        * 100.0 / A.Appeared   ELSE 0 END
              + CASE WHEN A.TotalMarks > 0 THEN A.ObtainedMarks * 100.0 / A.TotalMarks ELSE 0 END
            ) / 2.0
        AS DECIMAL(5,2))                      AS [Percent]
    FROM        Agg        A
    INNER JOIN  dbo.Employees  T  ON T.Id   = A.TeacherID
    INNER JOIN  dbo.SMSSubject SU ON SU.VID = A.SubjectID
    LEFT  JOIN  dbo.SMSClass   CL ON CL.VID = A.ClassID
    LEFT  JOIN  dbo.SMSSection SE ON SE.VID = A.SectionID
    ORDER BY    T.VName, CL.VName, SU.VName;
END
GO

SELECT 'Academy subject allocation : ' + CASE WHEN OBJECT_ID('dbo.SMSAcademyStaffSubject') IS NULL THEN 'missing' ELSE 'ready' END;
GO
