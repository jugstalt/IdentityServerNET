namespace IdentityServerNET.Services.PasswordHasher;

public class PasswordHashingOptions
{
    /// <summary>
    /// Template for the string that gets fed into the hash function.
    /// Supported placeholders (all replaced with lowercase values from the user profile):
    ///   {password}  — the plain-text password provided by the user (required)
    ///   {email}     — user.Email
    ///   {username}  — user.UserName
    /// Default: "{password}" — standard behaviour, no additional input.
    /// Example for a legacy system that appended the username:
    ///   "{password}{username}"
    /// </summary>
    public string Template { get; set; } = "{password}";
}
