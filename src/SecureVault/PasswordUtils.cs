using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault
{
    public static class PasswordUtils
    {
        // The full path of the file where the master password verifier is stored.
        // DataPaths decides the folder, so it only needs to be changed in one place.
        private static string MasterFile => DataPaths.MasterFile;

        // =========================
        // SALT GENERATION
        // =========================

        // Generates a random salt of the specified size in bytes
        // A salt is a random value added to a password before hashing
        // so that two users with the same password get completely different hashes
        public static byte[] GenerateSalt(int size = 16)
        {
            // Create an empty byte array to hold the salt
            var salt = new byte[size];

            // RandomNumberGenerator is cryptographically secure
            // unlike System.Random which is predictable and seeded by time
            // Predictable random numbers would make the salt useless
            using (var rng = RandomNumberGenerator.Create())
            {
                // Fill the array with unpredictable random bytes
                rng.GetBytes(salt);
            }
            return salt;
        }

        // =========================
        // KEY DERIVATION AND VERIFIER
        // =========================

        // The number of PBKDF2 rounds used for new vaults. Stored in master.dat,
        // so it can be raised later without breaking vaults created at a lower count.
        public const int DefaultIterations = 100_000;

        // First field of a version 2 master.dat. Chosen negative so it can never be
        // mistaken for the salt length that begins the old, version 1 format.
        private const int FormatVersion = -2;

        // Turns the master password into the 32-byte AES key using PBKDF2-HMAC-SHA256.
        // This is the only place the password is ever converted into key material.
        public static byte[] DeriveEncryptionKey(string password, byte[] salt, int iterations, int size = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(size);
            }
        }

        // The value written to disk to check a login. It is a SHA-256 of the key,
        // never the key itself, so reading master.dat does not hand over the vault key.
        // Recovering the key from the verifier still means guessing the password through PBKDF2.
        public static byte[] ComputeVerifier(byte[] key)
        {
            return SHA256.HashData(key);
        }

        // =========================
        // SAVE MASTER FILE (version 2)
        // =========================
        // Layout: int32 -2 | int32 iterations | int32 salt length | salt | int32 verifier length | verifier
        public static void SaveMaster(byte[] salt, byte[] verifier, int iterations)
        {
            if (salt == null || verifier == null)
                throw new ArgumentNullException("Salt or verifier cannot be null.");

            using (var fs = new FileStream(MasterFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(FormatVersion);
                bw.Write(iterations);
                bw.Write(salt.Length);
                bw.Write(salt);
                bw.Write(verifier.Length);
                bw.Write(verifier);
                bw.Flush();
            }
        }

        // =========================
        // LOAD MASTER FILE (versions 1 and 2)
        // =========================
        // Version 1 files (salt length | salt | hash length | hash) are still read.
        // For those, "verifier" holds the old stored value, which was the key itself,
        // and legacy is set to true so the caller can migrate the file on the next successful login.
        public static bool LoadMaster(out byte[] salt, out byte[] verifier, out int iterations, out bool legacy)
        {
            salt = null!;
            verifier = null!;
            iterations = DefaultIterations;
            legacy = false;

            if (!File.Exists(MasterFile))
                return false;

            try
            {
                using (var fs = new FileStream(MasterFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < 8)
                        return false;

                    int first = br.ReadInt32();
                    if (first == FormatVersion)
                    {
                        iterations = br.ReadInt32();
                        if (iterations <= 0)
                            return false;
                        first = br.ReadInt32();
                    }
                    else
                    {
                        legacy = true;
                    }

                    int saltLen = first;
                    if (saltLen <= 0 || saltLen > fs.Length - fs.Position)
                        return false;
                    salt = br.ReadBytes(saltLen);
                    if (salt.Length != saltLen)
                        return false;

                    if (fs.Position + 4 > fs.Length)
                        return false;
                    int verLen = br.ReadInt32();
                    if (verLen <= 0 || verLen > fs.Length - fs.Position)
                        return false;
                    verifier = br.ReadBytes(verLen);
                    if (verifier.Length != verLen)
                        return false;
                }
                return true;
            }
            catch (Exception)
            {
                salt = null!;
                verifier = null!;
                return false;
            }
        }

        // =========================
        // VERIFY PASSWORD
        // =========================
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        // Derives the key from the typed password and checks it against what is on disk.
        // Returns the key on success so the caller does not run PBKDF2 a second time,
        // or null if the password is wrong.
        public static byte[]? VerifyAndDeriveKey(string password, byte[] salt, byte[] storedVerifier,
            int iterations, bool legacy)
        {
            byte[] key = DeriveEncryptionKey(password, salt, iterations);
            byte[] candidate = legacy ? key : ComputeVerifier(key);
            return FixedTimeEquals(candidate, storedVerifier) ? key : null;
        }

        // =========================
        // ENCRYPT STRING
        // =========================

        // Encrypts a plain text string using AES 256 and returns the result
        // as a byte array containing the IV followed by the ciphertext
        // AES 256 is the industry standard for symmetric encryption
        public static byte[] EncryptString(string plainText, byte[] key)
        {
            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            using (var aes = Aes.Create())
            {
                aes.Key = key;

                // Generate a new random IV for every single encryption operation
                // IV stands for Initialisation Vector, it is a random value that
                // ensures the same password encrypted twice looks completely different
                // This prevents an attacker spotting patterns in the vault file
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    // Write the IV to the front of the output
                    // The IV does not need to be secret, only unique
                    // Writing it here means it is always available when decrypting
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var encryptor = aes.CreateEncryptor())

                        // CryptoStream applies the encryption as data is written through it
                        using (var cs = new CryptoStream(ms, encryptor,
                        CryptoStreamMode.Write))

                        // StreamWriter converts the string to bytes before writing
                        using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                        // When StreamWriter and CryptoStream are disposed here
                        // they flush and finalise the encrypted output
                    }

                    // Return the IV and ciphertext together as one byte array
                    return ms.ToArray();
                }
            }
        }

        // =========================
        // DECRYPT BYTES
        // =========================

        // Decrypts a byte array that was previously encrypted by EncryptString
        // Reads the IV from the front of the data then decrypts the rest
        public static string DecryptBytes(byte[] cipherData, byte[] key)
        {
            if (cipherData == null)
                throw new ArgumentNullException(nameof(cipherData));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            using (var aes = Aes.Create())
            {
                aes.Key = key;

                // The IV size in bytes is the block size in bits divided by 8
                // For AES this is always 16 bytes
                int ivSize = aes.BlockSize / 8;

                // If the data is shorter than the IV it cannot be valid
                if (cipherData.Length < ivSize)
                    throw new ArgumentException("Cipher data too short or invalid.");

                // Extract the IV from the front of the byte array
                // EncryptString wrote it there so we know exactly where it is
                byte[] iv = new byte[ivSize];
                Array.Copy(cipherData, iv, iv.Length);
                aes.IV = iv;

                // Read the ciphertext starting after the IV
                using (var ms = new MemoryStream(cipherData, iv.Length,
                    cipherData.Length - iv.Length))
                    using (var decryptor = aes.CreateDecryptor())

                    // CryptoStream applies decryption as data is read through it
                    using (var cs = new CryptoStream(ms, decryptor,
                    CryptoStreamMode.Read))

                    // StreamReader converts the decrypted bytes back to a string
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
