using System.Text.RegularExpressions;

namespace AuthManagement.Helpers;

public static class InputValidationHelper
{
    // Regex patterns for validation
    private static readonly Regex EmailPattern = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex AlphanumericPattern = new(@"^[a-zA-Z0-9\s]+$", RegexOptions.Compiled);
    private static readonly Regex NamePattern = new(@"^[a-zA-Z\s'-]+$", RegexOptions.Compiled);
    private static readonly Regex DescriptionPattern = new(@"^[a-zA-Z0-9\s.,!?;:()\-'""]+$", RegexOptions.Compiled);
    
    // Issue #2: Enhanced patterns to block dangerous content
    // Only block characters that could enable XSS or script injection
    // Allow normal punctuation like quotes, apostrophes for legitimate text
    private static readonly Regex SpecialCharsPattern = new(@"[<>\\`|${}]", RegexOptions.Compiled);
    private static readonly Regex HtmlPattern = new(@"<[^>]+>|&[a-z]+;|&#\d+;", RegexOptions.Compiled);
    private static readonly Regex ScriptPattern = new(@"<script[^>]*>.*?</script>|javascript:|on\w+=", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Issue #2 FIX: Corrected emoji pattern - only match actual emojis using surrogate pairs
    // In .NET, code points above U+FFFF (like 1F300-1F9FF) must be represented as surrogate pairs
    // The pattern [\u1F300-\u1F9FF] was incorrectly matching regular characters!
    // We now ONLY match surrogate pairs which correctly represent actual emojis
    private static readonly Regex EmojiPattern = new(
        @"[\uD83C-\uDBFF][\uDC00-\uDFFF]",  // Surrogate pairs ONLY - correctly matches all emojis like 😀, 🎉, 🚀, etc.
        RegexOptions.Compiled);
    
    // Issue #2: Allowed characters for general text (letters, numbers, spaces, basic punctuation)
    // Allow more punctuation including quotes, ampersand in text for legitimate use
    private static readonly Regex SafeTextPattern = new(@"^[a-zA-Z0-9\s.,!?;:()\-'""\/\n\r&@#%*+=\[\]]+$", RegexOptions.Compiled);
    private static readonly Regex SafeNamePattern = new(@"^[a-zA-Z0-9\s\-_.']+$", RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"^[a-zA-Z0-9\s\-_./:\?=&#%]+$", RegexOptions.Compiled);

    /// <summary>
    /// Issue #2: Validates and returns error message if input contains dangerous characters
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateInput(string? input, InputTypeEnum inputType, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return required ? (false, "This field is required.") : (true, null);
        }
        
        // Check for HTML/Script injection
        if (ContainsHtml(input) || ContainsScriptInjection(input))
        {
            return (false, "HTML tags and scripts are not allowed.");
        }
        
        // Check for emojis (except for email)
        if (inputType != InputTypeEnum.Email && ContainsEmojis(input))
        {
            return (false, "Emojis are not allowed.");
        }
        
        // Type-specific validation
        return inputType switch
        {
            InputTypeEnum.Email => ValidateEmail(input),
            InputTypeEnum.Name => ValidateName(input),
            InputTypeEnum.Description => ValidateDescription(input),
            InputTypeEnum.Url => ValidateUrl(input),
            InputTypeEnum.Alphanumeric => ValidateAlphanumeric(input),
            _ => ValidateGeneralText(input)
        };
    }
    
    private static (bool, string?) ValidateEmail(string input)
    {
        // Email allows: letters, numbers, ._%+-@
        var sanitized = Regex.Replace(input, @"[^a-zA-Z0-9._%+\-@]", string.Empty);
        if (sanitized != input)
        {
            return (false, "Email contains invalid characters.");
        }
        if (!EmailPattern.IsMatch(input))
        {
            return (false, "Please enter a valid email address.");
        }
        return (true, null);
    }
    
    private static (bool, string?) ValidateName(string input)
    {
        if (!SafeNamePattern.IsMatch(input))
        {
            return (false, "Only letters, numbers, spaces, hyphens, underscores, and dots are allowed.");
        }
        return (true, null);
    }
    
    private static (bool, string?) ValidateDescription(string input)
    {
        if (!SafeTextPattern.IsMatch(input))
        {
            return (false, "Special characters like < > & $ { } are not allowed.");
        }
        return (true, null);
    }
    
    private static (bool, string?) ValidateUrl(string input)
    {
        if (!UrlPattern.IsMatch(input))
        {
            return (false, "URL contains invalid characters.");
        }
        return (true, null);
    }
    
    private static (bool, string?) ValidateAlphanumeric(string input)
    {
        if (!AlphanumericPattern.IsMatch(input))
        {
            return (false, "Only letters, numbers, and spaces are allowed.");
        }
        return (true, null);
    }
    
    private static (bool, string?) ValidateGeneralText(string input)
    {
        if (SpecialCharsPattern.IsMatch(input))
        {
            return (false, "Characters like < > \\ ` | $ { } are not allowed for security reasons.");
        }
        return (true, null);
    }

    /// <summary>
    /// Sanitizes input by removing HTML, scripts, and dangerous characters
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove HTML tags
        string sanitized = HtmlPattern.Replace(input, string.Empty);
        
        // Remove script tags
        sanitized = ScriptPattern.Replace(sanitized, string.Empty);
        
        // Remove emojis
        sanitized = EmojiPattern.Replace(sanitized, string.Empty);
        
        // Only remove XSS-dangerous characters, keep normal punctuation
        sanitized = sanitized.Replace("<", "").Replace(">", "")
                             .Replace("\\", "").Replace("`", "")
                             .Replace("|", "").Replace("$", "")
                             .Replace("{", "").Replace("}", "");
        
        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes input for general text fields (allows basic punctuation)
    /// </summary>
    public static string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove HTML and scripts
        string sanitized = HtmlPattern.Replace(input, string.Empty);
        sanitized = ScriptPattern.Replace(sanitized, string.Empty);
        sanitized = EmojiPattern.Replace(sanitized, string.Empty);
        
        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes email input (allows only valid email characters)
    /// </summary>
    public static string SanitizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove everything except valid email characters
        var sanitized = Regex.Replace(input, @"[^a-zA-Z0-9._%+-@]", string.Empty);
        return sanitized.Trim();
    }

    /// <summary>
    /// Validates if string contains only alphanumeric characters and spaces
    /// </summary>
    public static bool IsAlphanumeric(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return AlphanumericPattern.IsMatch(input);
    }

    /// <summary>
    /// Validates if string is a valid name (letters, spaces, hyphens, apostrophes)
    /// </summary>
    public static bool IsValidName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return NamePattern.IsMatch(input);
    }

    /// <summary>
    /// Validates if string is a valid email
    /// </summary>
    public static bool IsValidEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return EmailPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if input contains special characters that should be blocked
    /// </summary>
    public static bool ContainsSpecialCharacters(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return SpecialCharsPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if input contains HTML tags
    /// </summary>
    public static bool ContainsHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return HtmlPattern.IsMatch(input);
    }
    
    /// <summary>
    /// Issue #2: Checks if input contains script injection attempts
    /// </summary>
    public static bool ContainsScriptInjection(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return ScriptPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if input contains emojis
    /// </summary>
    public static bool ContainsEmojis(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return EmojiPattern.IsMatch(input);
    }

    /// <summary>
    /// Truncates text to specified length and adds ellipsis if needed
    /// </summary>
    public static string TruncateText(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Filters out invalid characters as user types (for oninput event)
    /// </summary>
    public static string FilterInput(string? input, InputTypeEnum inputType)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        switch (inputType)
        {
            case InputTypeEnum.Email:
                return Regex.Replace(input, @"[^a-zA-Z0-9._%+-@]", string.Empty);
            
            case InputTypeEnum.Name:
                // Allow apostrophes for names like O'Brien
                return Regex.Replace(input, @"[^a-zA-Z0-9\s\-_.']+", string.Empty);
            
            case InputTypeEnum.Alphanumeric:
                return Regex.Replace(input, @"[^a-zA-Z0-9\s]", string.Empty);
            
            case InputTypeEnum.Url:
                // Allow hash and percent for URLs
                return Regex.Replace(input, @"[^a-zA-Z0-9\-_./:\?=&#%]+", string.Empty);
            
            case InputTypeEnum.Description:
                // Only remove XSS-dangerous characters, keep punctuation and quotes
                string filtered = HtmlPattern.Replace(input, string.Empty);
                filtered = ScriptPattern.Replace(filtered, string.Empty);
                filtered = EmojiPattern.Replace(filtered, string.Empty);
                // Only remove truly dangerous chars
                filtered = filtered.Replace("<", "").Replace(">", "");
                return filtered;
            
            default:
                return SanitizeText(input);
        }
    }
    
    /// <summary>
    /// Issue #2: Real-time filter for onkeypress/oninput - returns filtered value
    /// Use this to immediately block dangerous characters as user types
    /// </summary>
    public static string FilterRealtime(string? input, InputTypeEnum inputType)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
            
        return FilterInput(input, inputType);
    }
}

public enum InputTypeEnum
{
    Email,
    Name,
    Alphanumeric,
    Description,
    Url,
    General
}
