using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.API.Attributes;

/// <summary>
/// Attribute to sanitize input data to prevent XSS and injection attacks
/// </summary>
public class SanitizeInputAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument != null)
            {
                SanitizeObject(argument);
            }
        }

        base.OnActionExecuting(context);
    }

    private static void SanitizeObject(object obj)
    {
        if (obj == null) return;

        var properties = obj.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                var sanitizedValue = SanitizeString(value);
                property.SetValue(obj, sanitizedValue);
            }
        }
    }

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove potentially dangerous HTML tags and scripts
        var sanitized = Regex.Replace(input, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"<[^>]+>", "");
        
        // Remove SQL injection patterns
        sanitized = Regex.Replace(sanitized, @"('|(\')|;|--|/\*|\*/)", "");
        
        // Trim whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }
}