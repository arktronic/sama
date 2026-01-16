using System.Security.Cryptography;
using SAMA.Data.Services;

namespace SAMA.Tests.Unit.Data.Services;

[TestClass]
public class AesEncryptionServiceTests
{
    private const string TestKey = "test-encryption-key-123";
    private const string TestPlainText = "Hello, World!";

    [TestMethod]
    public void ConstructorShouldAcceptValidKey()
    {
        var service = new AesEncryptionService(TestKey);

        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void ConstructorShouldThrowExceptionWhenKeyIsNull()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => new AesEncryptionService(null!));
        Assert.AreEqual("key", exception.ParamName);
    }

    [TestMethod]
    public void ConstructorShouldThrowExceptionWhenKeyIsEmpty()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => new AesEncryptionService(string.Empty));
        Assert.AreEqual("key", exception.ParamName);
    }

    [TestMethod]
    public void ConstructorShouldThrowExceptionWhenKeyIsWhitespace()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => new AesEncryptionService("   "));
        Assert.AreEqual("key", exception.ParamName);
    }

    [TestMethod]
    public void EncryptShouldReturnBase64String()
    {
        var service = new AesEncryptionService(TestKey);

        var encrypted = service.Encrypt(TestPlainText);

        Assert.IsNotNull(encrypted);
        Assert.IsGreaterThan(0, encrypted.Length);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(encrypted);
        Assert.IsNotEmpty(bytes);
    }

    [TestMethod]
    public void EncryptShouldReturnDifferentValuesForSameInput()
    {
        var service = new AesEncryptionService(TestKey);

        var encrypted1 = service.Encrypt(TestPlainText);
        var encrypted2 = service.Encrypt(TestPlainText);

        Assert.AreNotEqual(encrypted1, encrypted2);
    }

    [TestMethod]
    public void EncryptShouldReturnEmptyStringForEmptyInput()
    {
        var service = new AesEncryptionService(TestKey);

        var encrypted = service.Encrypt(string.Empty);

        Assert.AreEqual(string.Empty, encrypted);
    }

    [TestMethod]
    public void EncryptShouldReturnNullForNullInput()
    {
        var service = new AesEncryptionService(TestKey);

        var encrypted = service.Encrypt(null!);

        Assert.IsNull(encrypted);
    }

    [TestMethod]
    public void DecryptShouldReturnOriginalPlainText()
    {
        var service = new AesEncryptionService(TestKey);
        var encrypted = service.Encrypt(TestPlainText);

        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(TestPlainText, decrypted);
    }

    [TestMethod]
    public void DecryptShouldWorkWithLongText()
    {
        var service = new AesEncryptionService(TestKey);
        var longText = string.Join(" ", Enumerable.Repeat("This is a longer text to test encryption.", 100));
        var encrypted = service.Encrypt(longText);

        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(longText, decrypted);
    }

    [TestMethod]
    public void DecryptShouldReturnEmptyStringForEmptyInput()
    {
        var service = new AesEncryptionService(TestKey);

        var decrypted = service.Decrypt(string.Empty);

        Assert.AreEqual(string.Empty, decrypted);
    }

    [TestMethod]
    public void DecryptShouldReturnNullForNullInput()
    {
        var service = new AesEncryptionService(TestKey);

        var decrypted = service.Decrypt(null!);

        Assert.IsNull(decrypted);
    }

    [TestMethod]
    public void DecryptShouldThrowExceptionForInvalidCipherText()
    {
        var service = new AesEncryptionService(TestKey);
        var invalidCipherText = "InvalidBase64!@#$";

        Assert.ThrowsExactly<FormatException>(() => service.Decrypt(invalidCipherText));
    }

    [TestMethod]
    public void DecryptShouldThrowExceptionForTooShortCipherText()
    {
        var service = new AesEncryptionService(TestKey);
        var tooShortCipherText = Convert.ToBase64String(new byte[10]); // Less than nonce + tag

        Assert.ThrowsExactly<CryptographicException>(() => service.Decrypt(tooShortCipherText));
    }

    [TestMethod]
    public void DecryptShouldThrowExceptionForTamperedCipherText()
    {
        var service = new AesEncryptionService(TestKey);
        var encrypted = service.Encrypt(TestPlainText);

        // Tamper with the ciphertext
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // Flip bits in last byte
        var tamperedCipherText = Convert.ToBase64String(bytes);

        Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => service.Decrypt(tamperedCipherText));
    }

    [TestMethod]
    public void DecryptShouldFailWithDifferentKey()
    {
        var service1 = new AesEncryptionService(TestKey);
        var service2 = new AesEncryptionService("different-key");
        var encrypted = service1.Encrypt(TestPlainText);

        Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => service2.Decrypt(encrypted));
    }

    [TestMethod]
    public void EncryptDecryptShouldWorkWithSpecialCharacters()
    {
        var service = new AesEncryptionService(TestKey);
        var specialText = "Hello! 🔐 @#$%^&*()";

        var encrypted = service.Encrypt(specialText);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(specialText, decrypted);
    }

    [TestMethod]
    public void EncryptDecryptShouldWorkWithMultilineText()
    {
        var service = new AesEncryptionService(TestKey);
        var multilineText = "Line 1\nLine 2\r\nLine 3\tTabbed";

        var encrypted = service.Encrypt(multilineText);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(multilineText, decrypted);
    }

    [TestMethod]
    public void SameKeyShouldProduceSameDecryptionResult()
    {
        var service1 = new AesEncryptionService(TestKey);
        var service2 = new AesEncryptionService(TestKey);
        var encrypted = service1.Encrypt(TestPlainText);

        var decrypted1 = service1.Decrypt(encrypted);
        var decrypted2 = service2.Decrypt(encrypted);

        Assert.AreEqual(decrypted1, decrypted2);
        Assert.AreEqual(TestPlainText, decrypted1);
    }

    [TestMethod]
    public void EncryptShouldProduceCiphertextLongerThanPlaintext()
    {
        var service = new AesEncryptionService(TestKey);
        var encrypted = service.Encrypt(TestPlainText);

        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Encrypted should be at least: nonce (12) + tag (16) + plaintext length
        Assert.IsGreaterThanOrEqualTo(12 + 16 + TestPlainText.Length, encryptedBytes.Length);
    }

    [TestMethod]
    public void DifferentKeysShouldProduceDifferentDerivedKeys()
    {
        var service1 = new AesEncryptionService("key1");
        var service2 = new AesEncryptionService("key2");

        var encrypted1 = service1.Encrypt(TestPlainText);
        var encrypted2 = service2.Encrypt(TestPlainText);

        // The same plaintext encrypted with different keys should not be decryptable
        Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => service1.Decrypt(encrypted2));
        Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => service2.Decrypt(encrypted1));
    }
}
