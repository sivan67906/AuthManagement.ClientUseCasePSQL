namespace AuthManagement.Helpers;

/// <summary>
/// Helper class for formatting error messages
/// </summary>
public static class ErrorMessageHelper
{
    /// <summary>
    /// Parses an error string that may contain multiple errors with various delimiters.
    /// Handles formats like:
    /// - "Unable to register user: error1;error2;error3"
    /// - "error1|||error2|||error3"
    /// - "error1;error2;error3"
    /// - "error1\nerror2\nerror3"
    /// </summary>
    private static List<string> ParseErrorString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();
            
        var workingString = input.Trim();
        
        // Check if string has a colon prefix pattern (like "Unable to register user: errors")
        var colonIndex = workingString.IndexOf(':');
        if (colonIndex > 0 && colonIndex < workingString.Length - 1)
        {
            var afterColon = workingString.Substring(colonIndex + 1).Trim();
            
            // Only extract if the part after colon contains delimiters
            if (afterColon.Contains("|||") || afterColon.Contains(";") || afterColon.Contains("\n"))
            {
                workingString = afterColon;
            }
        }
        
        // Try splitting by different delimiters in order of priority
        List<string> errors;
        
        if (workingString.Contains("|||"))
        {
            errors = workingString.Split("|||", StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
        }
        else if (workingString.Contains(";"))
        {
            errors = workingString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
        }
        else if (workingString.Contains("\n"))
        {
            errors = workingString.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
        }
        else
        {
            errors = new List<string> { workingString };
        }
        
        return errors;
    }

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
            
        // Expand all errors through ParseErrorString
        var expandedErrors = new List<string>();
        foreach (var error in errors)
        {
            expandedErrors.AddRange(ParseErrorString(error));
        }
        
        if (expandedErrors.Count == 0)
            return string.Empty;
            
        if (expandedErrors.Count == 1)
            return expandedErrors[0];
            
        var sb = new System.Text.StringBuilder();
        sb.Append("<ul class=\"toast-error-list\">");
        foreach (var error in expandedErrors)
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
            
        var errors = ParseErrorString(message);
        
        if (errors.Count == 0)
            return string.Empty;
            
        if (errors.Count == 1)
            return errors[0];
            
        var sb = new System.Text.StringBuilder();
        sb.Append("<ul class=\"toast-error-list\">");
        foreach (var error in errors)
        {
            sb.Append($"<li>{System.Net.WebUtility.HtmlEncode(error)}</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }
    
    /// <summary>
    /// Formats a list of errors as HTML bullet points from a string array.
    /// </summary>
    public static string FormatErrorsAsBullets(params string[] errors)
    {
        return FormatErrorsAsBullets(errors as IEnumerable<string>);
    }
}
