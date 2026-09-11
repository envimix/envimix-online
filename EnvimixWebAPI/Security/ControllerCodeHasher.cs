using System.Security.Cryptography;
using System.Text;

namespace EnvimixWebAPI.Security;

public static class ControllerCodeHasher
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int Iterations = 210_000;
    private const int DerivedKeyLength = 32;

    public static string GenerateCode()
        => string.Create(8, 0, static (characters, _) =>
        {
            for (var index = 0; index < characters.Length; index++)
            {
                characters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
        });

    public static (string Hash, string Salt) Hash(string controllerCode)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Derive(controllerCode, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string controllerCode, string hash, string salt)
    {
        try
        {
            var expectedHash = Convert.FromBase64String(hash);
            var actualHash = Derive(controllerCode, Convert.FromBase64String(salt));
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string controllerCode, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(controllerCode), salt, Iterations, HashAlgorithmName.SHA512, DerivedKeyLength);
}
