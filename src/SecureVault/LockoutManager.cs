using System;
using System.IO;

namespace SecureVault
{
    public class LockoutInfo
    {
        public int FailedCount { get; set; }
        public DateTime LastFailed { get; set; }
    }

    public static class LockoutManager
    {
        private const string LockoutFile = "lockout.dat";

        // Backoff parameters
        private const int BaseDelaySeconds = 5;    // delay for first increment
        private const int MaxDelaySeconds = 300;   // maximum wait (5 minutes)

        public static LockoutInfo Load()
        {
            if (!File.Exists(LockoutFile)) return new LockoutInfo { FailedCount = 0, LastFailed = DateTime.MinValue };

            try
            {
                using (var fs = new FileStream(LockoutFile, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    int failedCount = br.ReadInt32();
                    long ticks = br.ReadInt64();
                    return new LockoutInfo { FailedCount = failedCount, LastFailed = new DateTime(ticks) };
                }
            }
            catch
            {
                // If file corrupted, reset
                return new LockoutInfo { FailedCount = 0, LastFailed = DateTime.MinValue };
            }
        }

        public static void Save(LockoutInfo info)
        {
            using (var fs = new FileStream(LockoutFile, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(info.FailedCount);
                bw.Write(info.LastFailed.Ticks);
            }
        }

        public static void RegisterFailedAttempt()
        {
            var info = Load();
            info.FailedCount = info.FailedCount + 1;
            info.LastFailed = DateTime.UtcNow;
            Save(info);
        }

        public static void Reset()
        {
            var info = new LockoutInfo { FailedCount = 0, LastFailed = DateTime.MinValue };
            Save(info);
        }

        // Returns remaining seconds to wait (0 if allowed now)
        public static int GetRemainingLockoutSeconds()
        {
            var info = Load();
            if (info.FailedCount <= 0) return 0;

            // exponential backoff: delay = BaseDelay * 2^(FailedCount-1)
            double delay = BaseDelaySeconds * Math.Pow(2, info.FailedCount - 1);
            if (delay > MaxDelaySeconds) delay = MaxDelaySeconds;

            DateTime windowStart = info.LastFailed;
            double elapsed = (DateTime.UtcNow - windowStart).TotalSeconds;
            int remaining = (int)Math.Ceiling(delay - elapsed);
            return remaining > 0 ? remaining : 0;
        }
    }
}
