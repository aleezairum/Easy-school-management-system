/* ============================================================================
   Why is the WhatsApp auto-reply not answering?  READ ONLY - changes nothing.
   Run on the live database (SSMS, or: sqlcmd -S . -d EasySchoolDB -i Check_WhatsAppAutoReply.sql)
   and read the four parts in order:

   1. Last messages IN from parents. Nothing in the last hours although parents
      are writing  ->  Meta is not reaching the server (webhook): the site or its
      https address is down / changed, or IIS / the API is stopped.
   2. Each recent message IN and what was sent back. A message IN with no reply
      after it  ->  the bot failed while composing; the API log says why
      (logs\stdout_*.log, lines with [WA-BOT]) - usually a table or column the
      new code needs that the database does not have: run the deploy runner.
   3. Replies that failed, with Meta's reason (error code and message).
   4. The tables and columns the WhatsApp menu reads.
   ============================================================================ */
SET NOCOUNT ON;

PRINT '1) Last 15 messages IN (from parents / staff)';
SELECT TOP 15 M.MessageTime AS TimeUtc, M.WaId, M.MessageKind, LEFT(M.Body, 60) AS Body
FROM WaMessages M WHERE M.Direction = 0
ORDER BY M.VID DESC;

PRINT '2) Last 20 messages IN and the first reply sent after each (NULL reply = the bot did not answer)';
SELECT TOP 20 I.MessageTime AS InUtc, I.WaId, LEFT(I.Body, 40) AS Asked,
       R.MessageTime AS ReplyUtc, R.Status AS ReplyStatus, R.ErrorCode, LEFT(R.ErrorMessage, 120) AS ReplyError
FROM WaMessages I
OUTER APPLY (SELECT TOP 1 O.MessageTime, O.Status, O.ErrorCode, O.ErrorMessage
             FROM WaMessages O
             WHERE O.Direction = 1 AND O.WaId = I.WaId AND O.VID > I.VID
             ORDER BY O.VID) R
WHERE I.Direction = 0 AND I.MessageKind IN ('text', 'interactive')
ORDER BY I.VID DESC;

PRINT '3) Failed replies in the last 3 days, by reason';
SELECT O.ErrorCode, LEFT(O.ErrorMessage, 150) AS Reason, COUNT(*) AS Times, MAX(O.MessageTime) AS LastUtc
FROM WaMessages O
WHERE O.Direction = 1 AND O.Status = 'failed' AND O.MessageTime >= DATEADD(DAY, -3, SYSUTCDATETIME())
GROUP BY O.ErrorCode, LEFT(O.ErrorMessage, 150)
ORDER BY Times DESC;

PRINT '4) What the WhatsApp menu needs (MISSING = run the deploy runner)';
SELECT Item, CASE WHEN Ok = 1 THEN 'ok' ELSE 'MISSING' END AS State FROM (VALUES
    ('TABLE SMSExamFinalize (result card / term result)',  CASE WHEN OBJECT_ID('dbo.SMSExamFinalize') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE SMSDailyDiary (today''s diary)',              CASE WHEN OBJECT_ID('dbo.SMSDailyDiary') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE SMSDailyDiaryImage',                           CASE WHEN OBJECT_ID('dbo.SMSDailyDiaryImage') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE SMSAnnouncement (announcements)',              CASE WHEN OBJECT_ID('dbo.SMSAnnouncement') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE SMSClassTiming (school timings)',              CASE WHEN OBJECT_ID('dbo.SMSClassTiming') IS NOT NULL THEN 1 ELSE 0 END),
    ('VIEW  vwActiveFeePromise (dues)',                    CASE WHEN OBJECT_ID('dbo.vwActiveFeePromise') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE HrPayrollRun (staff salary slip)',             CASE WHEN OBJECT_ID('dbo.HrPayrollRun') IS NOT NULL THEN 1 ELSE 0 END),
    ('COLUMN HrPayslip.Fine (salary slip)',                CASE WHEN COL_LENGTH('dbo.HrPayslip', 'Fine') IS NOT NULL THEN 1 ELSE 0 END),
    ('TABLE HrStaffAdvance (salary slip)',                 CASE WHEN OBJECT_ID('dbo.HrStaffAdvance') IS NOT NULL THEN 1 ELSE 0 END)
) X(Item, Ok);
