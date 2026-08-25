using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault
{
    public static class PasswordUtils
    {
        private const string MasterFile = "master.dat";

        // =========================
        // SALT GENERATION
        // =========================
        public static byte[] GenerateSalt(int size = 16)
        {
            var salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // =========================
        // HASHING
        // =========================
        public static byte[] HashPassword(string password, byte[] salt, int iterations = 100000, int size = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(size);
            }
        }

        // =========================
        // SAVE MASTER HASH
        // =========================
        public static void SaveMasterHash(byte[] salt, byte[] hash)
        {
            if (salt == null || hash == null)
                throw new ArgumentNullException("Salt or hash cannot be null.");

            using (var fs = new FileStream(MasterFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(salt.Length);
                bw.Write(salt);
                bw.Write(hash.Length);
                bw.Write(hash);
                bw.Flush();
            }
        }

        // =========================
        // LOAD MASTER HASH (Safe)
        // =========================
        public static bool LoadMasterHash(out byte[] salt, out byte[] hash)
        {
            salt = null;
            hash = null;

            if (!File.Exists(MasterFile))
                return false;

            try
            {
                using (var fs = new FileStream(MasterFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    // Validate that we at least have 8 bytes for the two length fields
                    if (fs.Length < 8)
                        return false;

                    int saltLen = br.ReadInt32();
                    if (saltLen < 0 || saltLen > fs.Length - fs.Position)
                        return false;

                    salt = br.ReadBytes(saltLen);
                    if (salt.Length != saltLen)
                        return false;

                    if (fs.Position + 4 > fs.Length)
                        return false;

                    int hashLen = br.ReadInt32();
                    if (hashLen < 0 || hashLen > fs.Length - fs.Position)
                        return false;

                    hash = br.ReadBytes(hashLen);
                    if (hash.Length != hashLen)
                        return false;
                }

                return true;
            }
            catch (EndOfStreamException)
            {
                salt = null;
                hash = null;
                return false;
            }
            catch (IOException)
            {
                salt = null;
                hash = null;
                return false;
            }
            catch (Exception)
            {
                salt = null;
                hash = null;
                return false;
            }
        }

        // =========================
        // VERIFY PASSWORD
        // =========================
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public static bool VerifyPassword(string password, byte[] salt, byte[] storedHash)
        {
            byte[] hash = HashPassword(password, salt);
            return FixedTimeEquals(hash, storedHash);
        }


        // =========================
        // DERIVE ENCRYPTION KEY
        // =========================
        public static byte[] DeriveEncryptionKey(string password, byte[] salt, int size = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(size);
            }
        }

        // =========================
        // ENCRYPT STRING
        // =========================
        public static byte[] EncryptString(string plainText, byte[] key)
        {
            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    // Write IV first
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var encryptor = aes.CreateEncryptor())
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }

                    return ms.ToArray();
                }
            }
        }

        // =========================
        // DECRYPT BYTES
        // =========================
        public static string DecryptBytes(byte[] cipherData, byte[] key)
        {
            if (cipherData == null)
                throw new ArgumentNullException(nameof(cipherData));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            using (var aes = Aes.Create())
            {
                aes.Key = key;

                int ivSize = aes.BlockSize / 8;
                if (cipherData.Length < ivSize)
                    throw new ArgumentException("Cipher data too short or invalid.");

                byte[] iv = new byte[ivSize];
                Array.Copy(cipherData, iv, iv.Length);
                aes.IV = iv;

                using (var ms = new MemoryStream(cipherData, iv.Length, cipherData.Length - iv.Length))
                using (var decryptor = aes.CreateDecryptor())
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
