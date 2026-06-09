using System.Collections.Concurrent;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class SmsProgressTracker : ISmsProgressTracker
    {
        private static readonly TimeSpan RetainAfterComplete = TimeSpan.FromMinutes(15);
        private readonly ConcurrentDictionary<string, SmsProgressDto> _jobs = new();

        public void Start(string jobId, int total)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return;
            PurgeStale();

            var now = DateTime.UtcNow;
            _jobs[jobId] = new SmsProgressDto
            {
                JobId = jobId,
                Total = total,
                Sent = 0,
                Failed = 0,
                IsComplete = false,
                StartedAt = now,
                UpdatedAt = now
            };
        }

        public void SetTotal(string jobId, int total)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return;
            if (_jobs.TryGetValue(jobId, out var snap))
            {
                lock (snap)
                {
                    snap.Total = total;
                    snap.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        public void IncrementSent(string jobId) => Bump(jobId, sent: true);
        public void IncrementFailed(string jobId) => Bump(jobId, sent: false);

        public void Complete(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return;
            if (_jobs.TryGetValue(jobId, out var snap))
            {
                lock (snap)
                {
                    snap.IsComplete = true;
                    snap.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        public SmsProgressDto? Get(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return null;
            if (!_jobs.TryGetValue(jobId, out var snap)) return null;

            lock (snap)
            {
                return new SmsProgressDto
                {
                    JobId = snap.JobId,
                    Total = snap.Total,
                    Sent = snap.Sent,
                    Failed = snap.Failed,
                    IsComplete = snap.IsComplete,
                    StartedAt = snap.StartedAt,
                    UpdatedAt = snap.UpdatedAt
                };
            }
        }

        private void Bump(string jobId, bool sent)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return;
            if (!_jobs.TryGetValue(jobId, out var snap)) return;

            lock (snap)
            {
                if (sent) snap.Sent++;
                else snap.Failed++;
                snap.UpdatedAt = DateTime.UtcNow;
            }
        }

        private void PurgeStale()
        {
            var cutoff = DateTime.UtcNow - RetainAfterComplete;
            foreach (var kvp in _jobs)
            {
                if (kvp.Value.IsComplete && kvp.Value.UpdatedAt < cutoff)
                    _jobs.TryRemove(kvp.Key, out _);
            }
        }
    }
}
