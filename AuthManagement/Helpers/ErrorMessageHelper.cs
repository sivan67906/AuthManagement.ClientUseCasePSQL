namespace AuthManagement.Helpers;

/// <summary>
/// Helper class for formatting error messages
/// </summary>
public static class ErrorMessageHelper
{
    /// <summary>
    /// Formats a list of errors as HTML bullet points for display in toast notifications.
    /// If only one error, returns the error as plain text.
    /// If multiple errors, returns an HTML unordered list with each error as a list item.
    /// Also handles semicolon-separated error strings.
    /// </summary>
    public static string FormatErrorsAsBullets(IEnumerable<string>? errors)
    {
        if (errors == null || !errors.Any())
            return string.Empty;
            
        var errorList = errors.ToList();
        
        // If single error contains semicolons or "|||", split it
        if (errorList.Count == 1)
        {
            var singleError = errorList[0];
            
            // Remove common prefixes like "Unable to register user: "
            var prefixPatterns = new[] { "Unable to register user: ", "Registration failed: ", "Validation failed: " };
            foreach (var prefix in prefixPatterns)
            {
                if (singleError.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    singleError = singleError.Substring(prefix.Length);
                    break;
                }
            }
            
            // Check for multiple errors in single string
            if (singleError.Contains("|||"))
            {
                errorList = singleError.Split("|||", StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();
            }
            else if (singleError.Contains(";"))
            {
                errorList = singleError.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();
            }
            else
            {
                return singleError; // Return single error as-is
            }
        }
        
        if (errorList.Count == 1)
            return errorList[0];
            
        var sb = new System.Text.StringBuilder();
        sb.Append("<ul class=\"toast-error-list\">");
        foreach (var error in errorList)
        {
            sb.Append($"<li>{System.Net.WebUtility.HtmlEncode(error)}</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }
    
    /// <summary>
    /// Formats a single message that may contain multiple semicolon or ||| separated errors.
    /// </summary>
    public static string FormatMessageAsBullets(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;
            
        return FormatErrorsAsBullets(new[] { message });
    }
    
    /// <summary>
    /// Formats a list of errors as HTML bullet points from a string array.
    /// </summary>
    public static string FormatErrorsAsBullets(params string[] errors)
    {
        return FormatErrorsAsBullets(errors as IEnumerable<string>);
    }
}
