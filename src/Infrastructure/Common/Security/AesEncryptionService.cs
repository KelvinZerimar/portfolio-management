using System.Security.Cryptography;
using Application.Common.Security;
using Infrastructure.Common.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Common.Security;

internal sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV is not secret — it's stored alongside the ciphertext so Decrypt can recover it.
        return Convert.ToBase64String([.. aes.IV, .. cipherBytes]);
    }

    public string Decrypt(string cipherText)
    {
        var combined = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        var iv = combined[..aes.IV.Length];
        var cipherBytes = combined[aes.IV.Length..];
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
