using System.Security.Cryptography;
using System.Text;

namespace SAMA.Data.Services;

public class AesEncryptionService
{
    private const int NonceSize = 12; // 96 bits is recommended for GCM
    private const int TagSize = 16; // 128 bits authentication tag
    private readonly byte[] _key;

    public AesEncryptionService(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Encryption key cannot be null or empty.", nameof(key));
        }

        // Hash the input string to get exactly 32 bytes (256 bits) for AES-256
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    public virtual string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Combine nonce + tag + ciphertext
        var result = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public virtual string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        var combinedData = Convert.FromBase64String(cipherText);

        if (combinedData.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Invalid ciphertext format.");
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[combinedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(combinedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combinedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(combinedData, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
