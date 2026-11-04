using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Classified.Shared.Libs.CodeHasher
{
    //public sealed class HmacCodeHasher : ICodeHasher
    //{
    //    private readonly byte[] _secretKey;

    //    public HmacCodeHasher(IOptions<CodeHasherOptions> options)
    //    {
    //        var secret = options.Value.Secret;

    //        if (string.IsNullOrWhiteSpace(secret))
    //        {
    //            throw new InvalidOperationException(
    //                "CodeHasher secret is not configured.");
    //        }

    //        try
    //        {
    //            _secretKey = Convert.FromBase64String(secret);
    //        }
    //        catch (FormatException ex)
    //        {
    //            throw new InvalidOperationException(
    //                "CodeHasher secret must be a valid Base64 string.", ex);
    //        }

    //        if (_secretKey.Length < 32)
    //        {
    //            throw new InvalidOperationException(
    //                "CodeHasher secret must contain at least 32 bytes.");
    //        }
    //    }

    //    public string Hash(string code)
    //    {
    //        ArgumentException.ThrowIfNullOrWhiteSpace(code);

    //        var codeBytes = Encoding.UTF8.GetBytes(code);

    //        var hash = HMACSHA256.HashData(_secretKey, codeBytes);

    //        return Convert.ToBase64String(hash);
    //    }

    //    public bool Verify(string code, string hash)
    //    {
    //        if (string.IsNullOrWhiteSpace(code) ||
    //            string.IsNullOrWhiteSpace(hash))
    //        {
    //            return false;
    //        }

    //        byte[] providedHash;

    //        try
    //        {
    //            providedHash = Convert.FromBase64String(hash);
    //        }
    //        catch (FormatException)
    //        {
    //            return false;
    //        }

    //        var expectedHash = HMACSHA256.HashData(
    //            _secretKey,
    //            Encoding.UTF8.GetBytes(code));

    //        return CryptographicOperations.FixedTimeEquals(
    //            expectedHash,
    //            providedHash);
    //    }
    //}

    public sealed class HmacCodeHasher : ICodeHasher
    {
        private readonly byte[] _secretKey;

        public HmacCodeHasher(IOptions<CodeHasherOptions> options)
        {
            var secret = options.Value.Secret;

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    "CodeHasher secret is not configured.");

            try
            {
                _secretKey = Convert.FromBase64String(secret);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "CodeHasher secret must be a valid Base64 string.", ex);
            }

            if (_secretKey.Length < 32)
                throw new InvalidOperationException(
                    "CodeHasher secret must contain at least 32 bytes.");
        }

        public string Hash(string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            byte[] hash = ComputeHash(code);

            return Convert.ToBase64String(hash);
        }

        public bool Verify(string code, string hash)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            byte[] providedHash;

            try
            {
                providedHash = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }

            if (providedHash.Length != 32)
                return false;

            byte[] expectedHash = ComputeHash(code);

            return CryptographicOperations.FixedTimeEquals(
                expectedHash,
                providedHash);
        }

        private byte[] ComputeHash(string code)
        {
            string normalizedCode = code.ToUpperInvariant();

            byte[] codeBytes = Encoding.UTF8.GetBytes(normalizedCode);

            return HMACSHA256.HashData(_secretKey, codeBytes);
        }
    }
}
