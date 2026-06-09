using School.API.DTOs;

namespace School.API.Services.Interfaces
{
    public interface ISmsProgressTracker
    {
        void Start(string jobId, int total);
        void SetTotal(string jobId, int total);
        void IncrementSent(string jobId);
        void IncrementFailed(string jobId);
        void Complete(string jobId);
        SmsProgressDto? Get(string jobId);
    }
}
