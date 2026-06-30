using System.Security.Cryptography;
using System.Text;

namespace IslamiApi.Services;

public static class PasswordHelper
{
    private const int SaltLength = 32;
    private const int Iterations = 100_000;
    private const int HashLength = 32; // SHA-256 output

    public static string HashPassword(string password)
    {
        var salt = GenerateSalt();
        var hash = Pbkdf2(password, salt, Iterations);
        return $"{Iterations}${salt}${hash}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 3) return false;

            var iterations = int.Parse(parts[0]);
            var salt = parts[1];
            var expectedHash = parts[2];

            var hash = Pbkdf2(password, salt, iterations);
            return ConstantTimeEqual(hash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    // ── Matches Dart's manual PBKDF2 (HMAC-SHA256, single-block)
    private static string Pbkdf2(string password, string salt, int iterations)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Encoding.UTF8.GetBytes(salt);

        // U1 = HMAC(password, salt || 0x00000001)
        var block = new byte[saltBytes.Length + 4];
        saltBytes.CopyTo(block, 0);
        block[^4] = 0; block[^3] = 0; block[^2] = 0; block[^1] = 1;

        using var hmac = new HMACSHA256(passwordBytes);
        var u = hmac.ComputeHash(block);
        var result = (byte[])u.Clone();

        for (int i = 1; i < iterations; i++)
        {
            u = hmac.ComputeHash(u);
            for (int j = 0; j < result.Length; j++)
                result[j] ^= u[j];
        }

        return Convert.ToBase64String(result)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url
    }

    private static string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(SaltLength);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static bool ConstantTimeEqual(string a, string b)
    {
        if (a.Length != b.Length) return false;
        int result = 0;
        for (int i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];
        return result == 0;
    }
}
