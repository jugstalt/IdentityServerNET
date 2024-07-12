using IdentityServer.Nova.Abstractions.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IdentityServer.Nova.Services.Cryptography;

public class DefaultCryptoService : ICryptoService
{
    public DefaultCryptoService(string cryptoPassword)
        : this(Encoding.UTF8.GetBytes(cryptoPassword))
    {

    }

    public DefaultCryptoService(byte[] passwordBytes)
    {
        _defaultPasswordBytes = passwordBytes;
        if (_defaultPasswordBytes.Length < 24)
        {
            throw new Exception("DefaultCryptoService Password is too short!");
        }
        DefaultEncoding = Encoding.UTF8;
    }

    public Encoding DefaultEncoding { get; set; }

    #region ICryptoService

    public byte[] _defaultPasswordBytes;

    public string EncryptText(string text, Encoding encoding = null)
    {
        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        var bytes = encoding == null ?
            DefaultEncoding.GetBytes(text) :
            encoding.GetBytes(text);


        return Convert.ToBase64String(AES_Encrypt(bytes, _defaultPasswordBytes, keySize: 128, useRandomSalt: true));
    }

    public string EncryptTextConvergent(string text, Encoding encoding = null)
    {
        try
        {
            byte[] passwordBytes = _defaultPasswordBytes.Take(24).ToArray();

            if (encoding == null)
            {
                encoding = DefaultEncoding;
            }

            var inputbuffer = Encoding.UTF8.GetBytes(text);

            SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create();
            ICryptoTransform transform = algorithm.CreateEncryptor(passwordBytes, _static_iv);

            var outputBuffer = transform.TransformFinalBlock(inputbuffer.ToArray(), 0, inputbuffer.Length);

            string result = String.Empty;

            return Convert.ToBase64String(outputBuffer);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public string DecryptText(string base64Text, Encoding encoding = null)
    {
        var decryptedBytes = AES_Decrypt(Convert.FromBase64String(base64Text), _defaultPasswordBytes,
            keySize: 128, useRandomSalt: true);

        if (encoding == null)
        {
            return DefaultEncoding.GetString(decryptedBytes);
        }

        return encoding.GetString(decryptedBytes);
    }

    public string DecryptTextConvergent(string base64Text, Encoding encoding = null)
    {
        try
        {
            var inputbuffer = Convert.FromBase64String(base64Text);

            byte[] passwordBytes = _defaultPasswordBytes.Take(24).ToArray();

            SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create();
            ICryptoTransform transform = algorithm.CreateDecryptor(passwordBytes, _static_iv);
            byte[] bytesDecrypted = transform.TransformFinalBlock(inputbuffer.ToArray(), 0, inputbuffer.Length);

            if (encoding == null)
            {
                encoding = DefaultEncoding;
            }

            string result = encoding.GetString(bytesDecrypted);

            return result;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    #endregion

    #region RSA

    public static byte[] EncryptDataOaepSha(X509Certificate2 cert, byte[] data, RSAEncryptionPadding strengh)
    {
        using (RSA rsa = cert.GetRSAPublicKey())
        {
            int pos = 0, blocksize = 128;  // max: 256 - padding
            List<byte> encrypted = new List<byte>();
            while (true)
            {
                var dataBlock = data.Skip(pos).Take(blocksize).ToArray();
                if (dataBlock.Length == 0)
                {
                    break;
                }

                encrypted.AddRange(rsa.Encrypt(dataBlock, strengh));

                pos += 128;
                if (dataBlock.Length < blocksize || pos >= data.Length)
                {
                    break;
                }
            }

            return encrypted.ToArray();
        }
    }

    public static byte[] DecryptDataOaepSha(X509Certificate2 cert, byte[] data, RSAEncryptionPadding strengh)
    {
        using (RSA rsa = cert.GetRSAPrivateKey())
        {
            int pos = 0, blocksize = 256; // fix

            List<byte> decrypted = new List<byte>();
            while (true)
            {
                var dataBlock = data.Skip(pos).Take(blocksize).ToArray();
                if (dataBlock.Length == 0)
                {
                    break;
                }

                decrypted.AddRange(rsa.Decrypt(dataBlock, strengh));

                pos += blocksize;
            }

            return decrypted.ToArray();
        }
    }

    public static string EncryptText(X509Certificate2 cert, string text, ResultType resultType = ResultType.Base64)
    {
        var data = EncryptDataOaepSha(cert, Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.OaepSHA256);

        switch (resultType)
        {
            case ResultType.Hex:
                return "0x" + string.Concat(data.Select(b => b.ToString("X2")));
            default: // base64
                return Convert.ToBase64String(data);
        }
    }

    public static string DecryptText(X509Certificate2 cert, string cipherText)
    {
        var data = DecryptDataOaepSha(cert, StringToByteArray(cipherText), RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(data);
    }

    #endregion

    #region AES

    #region AES Base

    private const int _saltSize = 4; //, _iterations = 1000;

    static private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true, byte[] salt = null, byte[] g1 = null)
    {
        byte[] encryptedBytes = null;

        if (useRandomSalt)
        {
            // Add Random Salt in front -> two ident objects will produce differnt results
            // Remove the Bytes after decryption
            byte[] randomSalt = GetRandomBytes();
            byte[] bytesToEncrpytWidhSalt = new byte[randomSalt.Length + bytesToBeEncrypted.Length];
            Buffer.BlockCopy(randomSalt, 0, bytesToEncrpytWidhSalt, 0, randomSalt.Length);
            Buffer.BlockCopy(bytesToBeEncrypted, 0, bytesToEncrpytWidhSalt, randomSalt.Length, bytesToBeEncrypted.Length);

            bytesToBeEncrypted = bytesToEncrpytWidhSalt;
        }

        using (MemoryStream ms = new MemoryStream())
        {
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                // Faster (store 4 bytes to generating IV...)
                byte[] ivInitialBytes = GetRandomBytes();
                ms.Write(ivInitialBytes, 0, _saltSize);

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

    static private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true, byte[] salt = null, byte[] g1 = null)
    {
        byte[] decryptedBytes = null;

        using (MemoryStream ms = new MemoryStream())
        {
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                // Faster get bytes for IV from 
                var ivInitialBytes = new byte[_saltSize];
                Buffer.BlockCopy(bytesToBeDecrypted, 0, ivInitialBytes, 0, _saltSize);

                AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8, salt: salt, g1: g1);

                AES.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeDecrypted, _saltSize, bytesToBeDecrypted.Length - _saltSize);
                    cs.Close();
                }
                decryptedBytes = ms.ToArray();
            }
        }

        if (useRandomSalt)
        {
            byte[] ret = new byte[decryptedBytes.Length - _saltSize];
            Buffer.BlockCopy(decryptedBytes, _saltSize, ret, 0, ret.Length);
            decryptedBytes = ret;
        }

        return decryptedBytes;
    }

    static private byte[] GetRandomBytes()
    {
        byte[] ba = new byte[_saltSize];
        RNGCryptoServiceProvider.Create().GetBytes(ba);
        return ba;
    }

    static private byte[] GetBytes(byte[] initialBytes, int size)
    {
        var ret = new byte[size];
        Buffer.BlockCopy(initialBytes, 0, ret, 0, Math.Min(initialBytes.Length, ret.Length));

        return ret;
    }

    private static byte[] _g1 = new Guid("956F94BF45B44609B243A0B744DFFBE3").ToByteArray();
    static private byte[] GetHashedBytes(byte[] initialBytes, int size, byte[] salt, byte[] g1)
    {
        var hash = SHA256.Create().ComputeHash(initialBytes);

        var ret = new byte[size];
        Buffer.BlockCopy(hash, 0, ret, 0, Math.Min(hash.Length, ret.Length));

        byte[] saltBytes = salt ?? new byte[] { 167, 123, 23, 12, 64, 198, 177, 114 };
        var key = new Rfc2898DeriveBytes(hash, g1 ?? _g1, 10); // 10 is enough for this...
        ret = key.GetBytes(size);

        return ret;
    }

    private static byte[] _static_iv = new byte[8] { 11, 125, 103, 99, 47, 56, 34, 67 };

    #endregion

    #endregion

    #region Bytes

    static public byte[] EncryptBytes(byte[] bytesToBeEncrypted, string password, Strength strength = Strength.AES128, bool useRandomSalt = true)
    {
        try
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            return AES_Encrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
        }
        catch (Exception ex)
        {
            throw new CryptoException(ex);
        }
    }

    static public byte[] DecryptBytes(byte[] bytesToBeEncrypted, string password, Strength strength = Strength.AES128, bool useRandomSalt = true)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

        return AES_Decrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
    }

    static private int GetKeySize(Strength strength)
    {
        switch (strength)
        {
            case Strength.AES128:
                return 128;
            case Strength.AES192:
                return 192;
            case Strength.AES256:
                return 256;
        }

        return 128;
    }

    #endregion

    #region Hash

    public static string Hash64(string password, string username = "")
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password + username?.Trim().ToLower());
        passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
        return Convert.ToBase64String(passwordBytes);
    }

    public static bool VerifyPassword(string cleanPassword, string hash, string username = "")
    {
        if (Hash64(cleanPassword + username?.Trim().ToLower()) == hash)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region General

    static private byte[] StringToByteArray(String input)
    {
        if (input.StartsWith("0x"))  // Base 16 (HEX)
        {
            input = input.Substring(2, input.Length - 2);

            int NumberChars = input.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
            }

            return bytes;
        }

        return Convert.FromBase64String(input);
    }

    static private bool IsHexString(string hex)
    {
        if (hex.StartsWith("0x"))
        {
            hex = hex.Substring(2);

            bool isHex;
            foreach (var c in hex)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                {
                    return false;
                }
            }
            return true;
        }
        else if (hex.StartsWith("_"))
        {
            hex = hex.Substring(1);

            bool isHex;
            foreach (var c in hex)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'z') ||
                         (c >= 'A' && c <= 'Z'));

                if (!isHex)
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    #endregion

    #region Classes Enums

    public enum ResultType
    {
        Base64,
        Hex
    }

    public enum Strength
    {
        AES128 = 1,
        AES192 = 2,
        AES256 = 3
    }

    #endregion
}
