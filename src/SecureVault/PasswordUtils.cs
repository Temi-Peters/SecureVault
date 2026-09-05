using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault
{
    public static class PasswordUtils
    {
        // The name of the file where the master password hash is stored
        // Stored as a constant so it only needs to be changed in one place
        private const string MasterFile = "master.dat";

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
        // HASHING
        // =========================

        // Hashes a password using PBKDF2 with SHA256
        // PBKDF2 is a key derivation function designed specifically
        // for password hashing because it is deliberately slow
        // making brute force attacks much harder
        public static byte[] HashPassword(string password, byte[] salt,
            int iterations = 100000, int size = 32)
        {
            // Rfc2898DeriveBytes is the .NET implementation of PBKDF2
            // 100,000 iterations means an attacker must run 100,000
            // SHA256 operations for every single password guess
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256))
            {
                // Returns a 32 byte derived key
                // The actual password is never stored, only this derived value
                return pbkdf2.GetBytes(size);
            }
        }

        // =========================
        // SAVE MASTER HASH
        // =========================

        // Saves the salt and hash to master.dat on disk
        // The format is: salt length, salt bytes, hash length, hash bytes
        // Writing the length before each field means the reader always knows
        // exactly how many bytes to read for each part
        public static void SaveMasterHash(byte[] salt, byte[] hash)
        {
            if (salt == null || hash == null)
                throw new ArgumentNullException("Salt or hash cannot be null.");

            // FileShare.None prevents any other process from reading or
            // writing the file while we are writing to it
            using (var fs = new FileStream(MasterFile, FileMode.Create,
                FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
            {
                // Write the length of the salt first so the reader
                // knows how many bytes to read for the salt
                bw.Write(salt.Length);
                bw.Write(salt);

                // Write the length of the hash first so the reader
                // knows how many bytes to read for the hash
                bw.Write(hash.Length);
                bw.Write(hash);

                // Flush forces all buffered data to be written to disk immediately
                bw.Flush();
            }
        }

        // =========================
        // LOAD MASTER HASH
        // =========================

        // Reads the salt and hash back from master.dat
        // Returns true if successful, false if the file does not exist
        // or if anything goes wrong reading it
        // The out parameters are set to the loaded values on success
        public static bool LoadMasterHash(out byte[] salt, out byte[] hash)
        {
            salt = null;
            hash = null;

            // If master.dat does not exist this is the first run
            // Return false to signal that no master password has been set yet
            if (!File.Exists(MasterFile))
                return false;

            try
            {
                using (var fs = new FileStream(MasterFile, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                    using (var br = new BinaryReader(fs))
                {
                    // Check there are at least 8 bytes in the file
                    // 4 bytes for salt length + 4 bytes for hash length minimum
                    // If not the file is empty or corrupted
                    if (fs.Length < 8)
                        return false;

                    // Read the salt length prefix
                    int saltLen = br.ReadInt32();

                    // Validate the salt length is sensible before reading
                    if (saltLen < 0 || saltLen > fs.Length - fs.Position)
                        return false;

                    // Read exactly as many bytes as the salt length says
                    salt = br.ReadBytes(saltLen);
                    // If we got fewer bytes than expected the file is truncated
                    if (salt.Length != saltLen)
                        return false;

                    // Check there are still at least 4 bytes left for the hash length
                    if (fs.Position + 4 > fs.Length)
                        return false;

                    // Read the hash length prefix
                    int hashLen = br.ReadInt32();

                    // Validate the hash length before reading
                    if (hashLen < 0 || hashLen > fs.Length - fs.Position)
                        return false;

                    // Read exactly as many bytes as the hash length says
                    hash = br.ReadBytes(hashLen);

                    // If we got fewer bytes than expected the file is truncated
                    if (hash.Length != hashLen)
                        return false;
                }

                return true;
            }
            catch (EndOfStreamException)
            {
                // File ended before we finished reading
                salt = null;
                hash = null;
                return false;
            }
            catch (IOException)
            {
                // Something went wrong accessing the file
                salt = null;
                hash = null;
                return false;
            }
            catch (Exception)
            {
                // Catch any other unexpected errors
                salt = null;
                hash = null;
                return false;
            }
        }

        // =========================
        // VERIFY PASSWORD
        // =========================

        // Compares two byte arrays in a way that always takes the same amount of time
        // regardless of where the first difference is found
        // A normal comparison stops as soon as it finds a mismatch which leaks
        // information about how close a guess is through the response time
        // This method prevents that timing attack
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            int diff = 0;

            // XOR each pair of bytes together and OR the result into diff
            // If any bytes differ then diff will end up non-zero
            // Crucially the loop always runs all the way through
            // so it always takes the same amount of time
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];

            // If diff is still zero every byte matched
            return diff == 0;
        }

        // Checks whether an entered password matches the stored hash
        // Hashes the entered password with the stored salt and compares
        // using the timing safe FixedTimeEquals method
        public static bool VerifyPassword(string password, byte[] salt,
            byte[] storedHash)
        {
            byte[] hash = HashPassword(password, salt);
            return FixedTimeEquals(hash, storedHash);
        }
        // =========================
        // DERIVE ENCRYPTION KEY
        // =========================

        // Derives a separate AES encryption key from the master password
        // This key is used to encrypt and decrypt vault entries
        // It is stored in memory only for the duration of the session
        // and is never written to disk
        public static byte[] DeriveEncryptionKey(string password,
            byte[] salt, int size = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, 100000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(size);
            }
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
