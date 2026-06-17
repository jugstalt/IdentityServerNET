using System;
using System.Linq;
using IdentityServerNET.Services.Cryptography;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for <see cref="PasswordGenerator"/>: generated passwords must respect the
/// requested length, satisfy complexity requirements and not be trivially predictable.
/// </summary>
public class PasswordGeneratorTests
{
    private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void GenerateSecurePassword_HasRequestedLength(int length)
    {
        var password = PasswordGenerator.GenerateSecurePassword(length);

        Assert.Equal(length, password.Length);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void GenerateSecurePassword_SatisfiesComplexityRequirements(int length)
    {
        var password = PasswordGenerator.GenerateSecurePassword(length);

        Assert.Contains(password, c => LowerCase.Contains(c));
        Assert.Contains(password, c => UpperCase.Contains(c));
        Assert.Contains(password, c => Digits.Contains(c));
        Assert.Contains(password, c => SpecialChars.Contains(c));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GenerateSecurePassword_WithNonPositiveLength_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => PasswordGenerator.GenerateSecurePassword(length));
    }

    [Fact]
    public void GenerateSecurePassword_ProducesDifferentPasswordsAcrossCalls()
    {
        var passwords = Enumerable.Range(0, 50)
            .Select(_ => PasswordGenerator.GenerateSecurePassword(20))
            .ToList();

        // With 20 characters from a large alphabet, collisions are astronomically unlikely.
        Assert.Equal(passwords.Count, passwords.Distinct().Count());
    }
}
