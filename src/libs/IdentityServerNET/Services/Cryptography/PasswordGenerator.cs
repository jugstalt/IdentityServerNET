using System;
using System.Text;

namespace IdentityServerNET.Services.Cryptography;
public class PasswordGenerator
{
    private static readonly Random random = new Random();

    public static string GenerateSecurePassword(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentException("The length of the password must be greater than 0.", nameof(length));
        }

        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        const string allChars = lowerCase + upperCase + digits + specialChars;

        var password = new StringBuilder();

        // Stellen sicher, dass das Passwort mindestens einen Kleinbuchstaben, einen Großbuchstaben, eine Ziffer und ein Sonderzeichen enthält
        password.Append(lowerCase[random.Next(lowerCase.Length)]);
        password.Append(upperCase[random.Next(upperCase.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(specialChars[random.Next(specialChars.Length)]);

        for (int i = 4; i < length; i++)
        {
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Mischen der Zeichen im Passwort für mehr Sicherheit
        var passwordChars = password.ToString().ToCharArray();
        Shuffle(passwordChars);

        return new string(passwordChars);
    }

    private static void Shuffle(char[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Swap(ref array[i], ref array[j]);
        }
    }

    private static void Swap(ref char a, ref char b)
    {
        char temp = a;
        a = b;
        b = temp;
    }
}
