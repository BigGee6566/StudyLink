using System;
using System.Security.Cryptography;

namespace StudyLink.Services.Security
{
    /// <summary>
    /// PBKDF2-SHA256 password hashing, ported from TutorConnect PasswordHelper (SEC-01/02).
    /// A plain-text password never leaves this class, is never logged and is never stored.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>Work factor for new passwords. Stored per row so it can be raised later.</summary>
        public const int DefaultIterations = 100000;

        public const int SaltBytes = 16;
        public const int SubkeyBytes = 32;

        public sealed class HashedPassword
        {
            public byte[] Hash { get; set; }
            public byte[] Salt { get; set; }
            public int Iterations { get; set; }
        }

        public static HashedPassword Create(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("A password is required.", nameof(password));

            var salt = RandomBytes(SaltBytes);
            return new HashedPassword
            {
                Salt = salt,
                Iterations = DefaultIterations,
                Hash = DeriveKey(password, salt, DefaultIterations, SubkeyBytes)
            };
        }

        public static bool Verify(string password, byte[] storedHash, byte[] storedSalt, int iterations)
        {
            if (string.IsNullOrEmpty(password) || storedHash == null || storedSalt == null || iterations <= 0)
                return false;

            return FixedTimeEquals(DeriveKey(password, storedSalt, iterations, storedHash.Length), storedHash);
        }

        /// <summary>True when a hash used a weaker work factor, so it is re-hashed on the next sign-in.</summary>
        public static bool NeedsRehash(int iterations) => iterations < DefaultIterations;

        public static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Constant-time comparison. CryptographicOperations.FixedTimeEquals does not exist on .NET
        /// Framework, so the XOR accumulator from TutorConnect is kept: it never exits early, which
        /// would otherwise leak how many leading bytes matched.
        /// </summary>
        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int length)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(length);
        }
    }
}
