using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Services.SigningCredential;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.Cryptography;

public class SigningCredentialCertStoreCryptoService : ICryptoService
{
    private ISigningCredentialCertificateStorage _validationKeyStorage;
    public SigningCredentialCertStoreCryptoService(ISigningCredentialCertificateStorage validationKeyStorage)
    {
        _validationKeyStorage = validationKeyStorage;
    }

    public string DecryptText(string base64Text, Encoding encoding = null)
    {
        encoding = encoding ?? Encoding.Unicode;

        string jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(base64Text));
        var encryptedObject = JsonConvert.DeserializeObject<EncryptedObject>(jsonString);

        return DecryptToStringAsync(encryptedObject).Result;
    }

    public string DecryptTextConvergent(string base64Text, Encoding encoding = null)
    {
        //
        // Simple Base64 Converting!!
        //

        var decryptedBytes = Convert.FromBase64String(base64Text);

        if (encoding == null)
        {
            return Encoding.Unicode.GetString(decryptedBytes);
        }

        return encoding.GetString(decryptedBytes);
    }

    public string EncryptText(string text, Encoding encoding = null)
    {
        encoding = encoding ?? Encoding.Unicode;
        var encryptedObject = EncryptAsync(encoding.GetBytes(text)).Result;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(encryptedObject)));
    }

    public string EncryptTextConvergent(string text, Encoding encoding = null)
    {
        //
        // Simple Base64 Converting!!
        //

        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        var bytes = encoding == null ?
            Encoding.Unicode.GetBytes(text) :
            encoding.GetBytes(text);

        return Convert.ToBase64String(bytes);
    }

    #region Encryption

    async private Task<EncryptedObject> EncryptAsync(string clearText)
    {
        return await EncryptAsync(Encoding.Unicode.GetBytes(clearText));
    }

    async private Task<EncryptedObject> EncryptAsync(byte[] data)
    {
        var randomCert = await _validationKeyStorage.GetRandomCertificateAsync(60);

        var password = PasswordFromCert(randomCert);
        var bytes = AES_Encrypt(data, password.AESPassword, salt: password.AESSalt, g1: password.AESG1);

        return new EncryptedObject()
        {
            CertSubject = randomCert.Subject,
            EncryptedData = bytes
        };
    }

    async private Task<string> DecryptToStringAsync(EncryptedObject encryptedObject)
    {
        return Encoding.Unicode.GetString(await DecryptToBytesAsync(encryptedObject));
    }

    async private Task<byte[]> DecryptToBytesAsync(EncryptedObject encryptedObject)
    {
        X509Certificate2 cert = await _validationKeyStorage.GetCertificateAsync(encryptedObject.CertSubject);
        if (cert == null)
        {
            throw new Exception("Can't decrypt object. Unknown encryption certificate");
        }

        var password = PasswordFromCert(cert);
        var bytes = AES_Decrypt(encryptedObject.EncryptedData, password.AESPassword, salt: password.AESSalt, g1: password.AESG1);

        return bytes;
    }

    #endregion

    #region Helper

    private PasswordBytes PasswordFromCert(X509Certificate2 cert)
    {
        // Generate some Password for eg Token Encryption now to improve performace
        var hash = new SHA1Managed().ComputeHash(cert.GetPublicKey());
        hash = cert.GetRSAPrivateKey().SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

        var password = new byte[128];
        var salt = new byte[8];
        var g1 = new byte[16];

        Buffer.BlockCopy(hash, 0, password, 0, password.Length);
        Buffer.BlockCopy(hash, password.Length, salt, 0, salt.Length);
        Buffer.BlockCopy(hash, password.Length + salt.Length, g1, 0, g1.Length);

        return new PasswordBytes()
        {
            AESPassword = password,
            AESSalt = salt,
            AESG1 = g1
        };
    }

    private class PasswordBytes
    {
        public byte[] AESPassword { get; set; }
        public byte[] AESSalt { get; set; }
        public byte[] AESG1 { get; set; }
    }

    static private byte[] GetRandomBytes(int size)
    {
        byte[] ba = new byte[size];
        RNGCryptoServiceProvider.Create().GetBytes(ba);
        return ba;
    }

    static private byte[] GetBytes(byte[] initialBytes, int size)
    {
        var ret = new byte[size];
        Buffer.BlockCopy(initialBytes, 0, ret, 0, Math.Min(initialBytes.Length, ret.Length));

        return ret;
    }

    static private byte[] GetHashedBytes(byte[] initialBytes, int size, byte[] salt, byte[] g1)
    {
        var hash = SHA256.Create().ComputeHash(initialBytes);

        var ret = new byte[size];
        Buffer.BlockCopy(hash, 0, ret, 0, Math.Min(hash.Length, ret.Length));

        byte[] saltBytes = salt;
        var key = new Rfc2898DeriveBytes(hash, g1, 10); // 10 is enough for this...
        ret = key.GetBytes(size);

        return ret;
    }

    #region Crypto

    static private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes, int keySize = 128, byte[] salt = null, byte[] g1 = null)
    {
        byte[] encryptedBytes = null;

        using (MemoryStream ms = new MemoryStream())
        {
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                // Faster (store 4 bytes to generating IV...)
                byte[] ivInitialBytes = GetRandomBytes(salt.Length);
                ms.Write(ivInitialBytes, 0, salt.Length);

                AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8, salt: salt, g1: g1);

                AES.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                    cs.Close();
                }
                encryptedBytes = ms.ToArray();
            }
        }

        return encryptedBytes;
    }

    static private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes, int keySize = 128, byte[] salt = null, byte[] g1 = null)
    {
        byte[] decryptedBytes = null;

        using (MemoryStream ms = new MemoryStream())
        {
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                // Faster get bytes for IV from 
                var ivInitialBytes = new byte[salt.Length];
                Buffer.BlockCopy(bytesToBeDecrypted, 0, ivInitialBytes, 0, salt.Length);

                AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8, salt: salt, g1: g1);

                AES.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeDecrypted, salt.Length, bytesToBeDecrypted.Length - salt.Length);
                    cs.Close();
                }
                decryptedBytes = ms.ToArray();
            }
        }

        return decryptedBytes;
    }


    #endregion

    #endregion

    #region Classes

    private class EncryptedObject
    {
        [JsonProperty("certSubject")]
        public string CertSubject { get; set; }

        [JsonProperty("encryptedData")]
        public byte[] EncryptedData;
    }

    #endregion
}
