using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Application.Common.Attributes;

/// <summary>
/// Validation attribute for email addresses with enhanced security
/// </summary>
public class SecureEmailAttribute : ValidationAttribute
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConsecutiveDotsRegex = new(@"\.{2,}", RegexOptions.Compiled);

    public override bool IsValid(object? value)
    {
        if (value is not string email)
            return false;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        if (email.Length > 254) // RFC 5321 limit
            return false;

        // Check for consecutive dots
        if (ConsecutiveDotsRegex.IsMatch(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be a valid email address.";
    }
}

/// <summary>
/// Validation attribute for strong passwords
/// </summary>
public class StrongPasswordAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;

    public override bool IsValid(object? value)
    {
        if (value is not string password)
            return false;

        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < MinLength)
            return false;

        if (RequireUppercase && !password.Any(char.IsUpper))
            return false;

        if (RequireLowercase && !password.Any(char.IsLower))
            return false;

        if (RequireDigit && !password.Any(char.IsDigit))
            return false;

        if (RequireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
            return false;

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        var requirements = new List<string>();
        
        if (RequireUppercase) requirements.Add("uppercase letter");
        if (RequireLowercase) requirements.Add("lowercase letter");
        if (RequireDigit) requirements.Add("digit");
        if (RequireSpecialChar) requirements.Add("special character");

        var requirementText = string.Join(", ", requirements);
        
        return $"The {name} field must be at least {MinLength} characters long and contain at least one {requirementText}.";
    }
}

/// <summary>
/// Validation attribute to prevent common injection patterns
/// </summary>
public class NoInjectionAttribute : ValidationAttribute
{
    private static readonly string[] DangerousPatterns = {
        "<script", "</script>", "javascript:", "vbscript:", "onload=", "onerror=",
        "eval(", "expression(", "url(", "import(", "document.cookie",
        "'; DROP TABLE", "'; DELETE FROM", "'; UPDATE", "'; INSERT INTO",
        "UNION SELECT", "OR 1=1", "AND 1=1", "' OR '1'='1", "\" OR \"1\"=\"1"
    };

    public override bool IsValid(object? value)
    {
        if (value is not string input)
            return true; // Allow non-string values

        if (string.IsNullOrEmpty(input))
            return true;

        var lowerInput = input.ToLowerInvariant();
        
        return !DangerousPatterns.Any(pattern => lowerInput.Contains(pattern.ToLowerInvariant()));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field contains potentially dangerous content.";
    }
}