using System;
using System.IO;

namespace SecureVault
{
    // Decides where the three vault files live on disk and makes sure
    // that folder exists before anything tries to read or write them.
    //
    // Up to 3.1.1 the files were opened by bare name ("master.dat"), which
    // means "whatever folder the process happened to start in". Launched from
    // a terminal that was the folder you were standing in. Launched by double
    // clicking, it could be a folder the user cannot write to, so the first
    // save failed and the application appeared to hang. Keeping the files in
    // the user's local application data folder removes that dependence on how
    // the application was started.
    public static class DataPaths
    {
        // On Windows this is %LOCALAPPDATA%\SecureVault, for example
        // C:\Users\<name>\AppData\Local\SecureVault
        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecureVault");

        public static string MasterFile   => Path.Combine(DataDirectory, "master.dat");
        public static string PasswordFile => Path.Combine(DataDirectory, "passwords.dat");
        public static string LockoutFile  => Path.Combine(DataDirectory, "lockout.dat");

        private static readonly string[] FileNames = { "master.dat", "passwords.dat", "lockout.dat" };

        // Creates the data folder and, on the first run after upgrading from
        // 3.1.1 or earlier, moves an existing vault into it so nothing is lost.
        // Legacy files are looked for next to the executable first, then in
        // the working directory, which are the two places older versions
        // could have written them. Nothing is migrated if a vault already
        // exists in the new location.
        public static void Initialise()
        {
            Directory.CreateDirectory(DataDirectory);

            if (File.Exists(MasterFile))
                return;

            foreach (string legacyDir in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                if (!File.Exists(Path.Combine(legacyDir, "master.dat")))
                    continue;

                foreach (string name in FileNames)
                {
                    string from = Path.Combine(legacyDir, name);
                    string to = Path.Combine(DataDirectory, name);

                    if (!File.Exists(from) || File.Exists(to))
                        continue;

                    // Copy first, then remove the original. Done in two steps
                    // rather than one Move so that a folder the application can
                    // read but not change (a Program Files install, say) still
                    // ends up with a complete copy in the data folder. If the
                    // original cannot be removed it is simply left behind, and
                    // the copy in the data folder is the one used from now on.
                    File.Copy(from, to);
                    try
                    {
                        File.Delete(from);
                    }
                    catch (Exception)
                    {
                        // Leaving the original in place is acceptable.
                    }
                }
                return;
            }
        }
    }
}
