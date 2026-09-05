using System;
using System.IO;

namespace SecureVault
{
    // This class holds the lockout state information
    // It is a simple data container with no logic of its own
    public class LockoutInfo
    {
        // How many times the user has entered the wrong password in a row
        public int FailedCount { get; set; }

        // The exact date and time of the most recent failed attempt
        // Used to calculate how much of the lockout delay has already passed
        public DateTime LastFailed { get; set; }
    }

    public static class LockoutManager
    {
        // The name of the file where lockout state is saved on disk
        // Saving to disk means the lockout survives the app being closed and reopened
        // Without this an attacker could simply restart the app to bypass the lockout
        private static string LockoutFile => DataPaths.LockoutFile;

        // How many seconds to wait after the first failed attempt
        private const int BaseDelaySeconds = 5;

        // The maximum number of seconds to ever wait
        // This prevents the lockout becoming a permanent denial of service
        // where a legitimate user can never get back in
        private const int MaxDelaySeconds = 300; // 5 minutes

        // Reads the current lockout state from disk
        // If the file does not exist yet returns a default state with no failures
        public static LockoutInfo Load()
        {
            if (!File.Exists(LockoutFile))
                return new LockoutInfo
            {
                FailedCount = 0,
                    LastFailed = DateTime.MinValue
            };

            try
            {
                using (var fs = new FileStream(LockoutFile,
                    FileMode.Open, FileAccess.Read))
                    using (var br = new BinaryReader(fs))
                {
                    // Read the failure count stored as a 32 bit integer
                    int failedCount = br.ReadInt32();

                    // Read the timestamp stored as ticks
                    // Ticks are the number of 100 nanosecond intervals
                    // since January 1st 0001, used to represent any date and time
                    long ticks = br.ReadInt64();

                    return new LockoutInfo
                    {
                        FailedCount = failedCount,
                            LastFailed = new DateTime(ticks)
                    };
                }
            }
            catch
            {
                // If the file is corrupted or unreadable for any reason
                // reset to a clean state rather than crashing
                return new LockoutInfo
                {
                    FailedCount = 0,
                        LastFailed = DateTime.MinValue
                };
            }
        }

        // Saves the current lockout state to disk
        // Called after every failed attempt and after a successful login
        public static void Save(LockoutInfo info)
        {
            using (var fs = new FileStream(LockoutFile,
                FileMode.Create, FileAccess.Write))
                using (var bw = new BinaryWriter(fs))
            {
                // Write the failure count as a 32 bit integer
                bw.Write(info.FailedCount);

                // Write the timestamp as ticks so it can be read back exactly
                bw.Write(info.LastFailed.Ticks);
            }
        }

        // Records a failed login attempt by incrementing the count
        // and saving the current time to disk
        public static void RegisterFailedAttempt()
        {
            var info = Load();

            // Increment the failure count
            info.FailedCount = info.FailedCount++;

            // Record exactly when this failure happened
            // This is used later to calculate how much time has passed
            info.LastFailed = DateTime.UtcNow;
            // Save to disk immediately so the lockout persists
            // even if the app is closed right now
            Save(info);
        }

        // Resets the lockout state back to zero after a successful login
        public static void Reset()
        {
            var info = new LockoutInfo
            {
                FailedCount = 0,
                    LastFailed = DateTime.MinValue
            };
            Save(info);
        }

        // Returns how many seconds the user must still wait before trying again
        // Returns 0 if they are allowed to attempt login right now
        public static int GetRemainingLockoutSeconds()
        {
            var info = Load();

            // No failed attempts on record so no wait is needed
            if (info.FailedCount <= 0) return 0;

            // Calculate the delay for this number of failures
            // The formula is BaseDelay multiplied by 2 to the power of failures minus 1
            // So: 1 failure = 5s, 2 failures = 10s, 3 = 20s, 4 = 40s and so on
            double delay = BaseDelaySeconds * Math.Pow(2, info.FailedCount - 1);

            // Never exceed the maximum delay
            if (delay > MaxDelaySeconds) delay = MaxDelaySeconds;

            // Work out how many seconds have already passed since the last failure
            DateTime windowStart = info.LastFailed;
            double elapsed = (DateTime.UtcNow - windowStart).TotalSeconds;

            // Subtract the elapsed time from the total delay
            // to get how many seconds are still remaining
            int remaining = (int)Math.Ceiling(delay - elapsed);

            // Return the remaining seconds, or 0 if the wait is already over
            return remaining > 0 ? remaining : 0;
        }
    }
}
