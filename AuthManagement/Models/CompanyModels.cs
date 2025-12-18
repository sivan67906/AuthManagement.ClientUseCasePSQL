namespace AuthManagement.Models;

// Enums matching backend
public enum CompanyStatus : short
{
    Draft = 1,
    Active = 2,
    Inactive = 3
}

public enum LegalStructure : short
{
    PrivateLimited = 1,
    PublicLimited = 2,
    LLP = 3,
    Partnership = 4,
    SoleProprietor = 5,
    Proprietorship = 6,
    Trust = 7,
    HUF = 8,
    Society = 9,
    Government = 10,
    Other = 11
}

public enum RoundingMode : short
{
    HalfUp = 1,
    HalfDown = 2,
    Ceiling = 3,
    Floor = 4,
    HalfEven = 5,
    Bankers = 6
}

// Company DTO
public class CompanyDto
{
    public Guid Id { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? ShortName { get; set; }
    public LegalStructure LegalStructure { get; set; }
    public string LegalStructureName => GetLegalStructureName(LegalStructure);
    public DateTime? IncorporationDate { get; set; }
    public Guid? ParentCompanyId { get; set; }
    public string? ParentCompanyName { get; set; }
    public CompanyStatus Status { get; set; }
    public string StatusName => Status.ToString();

    // Registration
    public string? RegistrationNumber { get; set; }
    public string? PANNumber { get; set; }
    public string? GSTIN { get; set; }
    public string? TANNumber { get; set; }
    public string? OtherTaxId { get; set; }
    public Guid RegistrationCountryId { get; set; }
    public string? RegistrationCountryName { get; set; }
    public Guid? RegistrationStateId { get; set; }
    public string? RegistrationStateName { get; set; }

    // Aliases for backward compatibility with Razor components
    public string? TAN
    {
        get => TANNumber;
        set => TANNumber = value;
    }
    public string? OtherTaxIdentifier
    {
        get => OtherTaxId;
        set => OtherTaxId = value;
    }

    // Address
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public Guid CityId { get; set; }
    public string? CityName { get; set; }
    public Guid StateId { get; set; }
    public string? StateName { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public string? CountryName { get; set; }
    public Guid TimeZoneId { get; set; }
    public string? TimeZoneName { get; set; }

    // Address aliases for backward compatibility with Razor components
    public Guid AddressCountryId
    {
        get => CountryId;
        set => CountryId = value;
    }
    public string? AddressCountryName
    {
        get => CountryName;
        set => CountryName = value;
    }
    public Guid AddressStateId
    {
        get => StateId;
        set => StateId = value;
    }
    public string? AddressStateName
    {
        get => StateName;
        set => StateName = value;
    }
    public Guid AddressCityId
    {
        get => CityId;
        set => CityId = value;
    }
    public string? AddressCityName
    {
        get => CityName;
        set => CityName = value;
    }

    // Contact
    public string? PrimaryContactName { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoFileUrl { get; set; }

    // Financial
    public Guid BaseCurrencyId { get; set; }
    public string? BaseCurrencyName { get; set; }
    public string? BaseCurrencyCode { get; set; }
    public Guid? ReportingCurrencyId { get; set; }
    public string? ReportingCurrencyName { get; set; }
    public string? ReportingCurrencyCode { get; set; }
    public byte FiscalYearStartMonth { get; set; }
    public DateTime? BooksStartDate { get; set; }
    public bool EnableMultiCurrency { get; set; }
    public byte RoundingPrecision { get; set; }
    public RoundingMode? RoundingMode { get; set; }
    public string? RoundingModeName => RoundingMode?.ToString();

    // Posting Controls
    public DateTime? AllowPostingFromDate { get; set; }
    public DateTime? AllowPostingToDate { get; set; }
    public bool LockBackDatedPosting { get; set; }
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Alias for backward compatibility with Razor components
    public string? UpdatedBy
    {
        get => ModifiedBy;
        set => ModifiedBy = value;
    }

    private static string GetLegalStructureName(LegalStructure structure) => structure switch
    {
        LegalStructure.PrivateLimited => "Private Limited",
        LegalStructure.PublicLimited => "Public Limited",
        LegalStructure.LLP => "LLP",
        LegalStructure.Partnership => "Partnership",
        LegalStructure.SoleProprietor => "Sole Proprietor",
        LegalStructure.Proprietorship => "Proprietorship",
        LegalStructure.Trust => "Trust",
        LegalStructure.HUF => "HUF",
        LegalStructure.Society => "Society",
        LegalStructure.Government => "Government",
        LegalStructure.Other => "Other",
        _ => structure.ToString()
    };
}

// Create Request
public class CreateCompanyRequest
{
    // Required fields (matching API)
    public string CompanyCode { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? ShortName { get; set; }
    public LegalStructure LegalStructure { get; set; } = LegalStructure.PrivateLimited;
    public DateTime? IncorporationDate { get; set; }
    public Guid? ParentCompanyId { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Draft;

    // Registration & Compliance (RegistrationCountryId is required)
    public string? RegistrationNumber { get; set; }
    public string? PANNumber { get; set; }
    public string? GSTIN { get; set; }
    public string? TANNumber { get; set; }
    public string? OtherTaxId { get; set; }
    public Guid RegistrationCountryId { get; set; } // Required - changed from nullable
    public Guid? RegistrationStateId { get; set; } // Optional - nullable

    // Aliases for backward compatibility with Razor components (excluded from serialization)
    [System.Text.Json.Serialization.JsonIgnore]
    public string? TAN
    {
        get => TANNumber;
        set => TANNumber = value;
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? OtherTaxIdentifier
    {
        get => OtherTaxId;
        set => OtherTaxId = value;
    }

    // Address (Required fields: AddressLine1, CityId, StateId, PostalCode, CountryId, TimeZoneId)
    public string AddressLine1 { get; set; } = string.Empty; // Required
    public string? AddressLine2 { get; set; }
    public Guid CityId { get; set; } // Required - changed from nullable
    public Guid StateId { get; set; } // Required - changed from nullable
    public string PostalCode { get; set; } = string.Empty; // Required
    public Guid CountryId { get; set; } // Required - changed from nullable
    public Guid TimeZoneId { get; set; } // Required

    // Address aliases for backward compatibility with Razor components (excluded from serialization)
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressCountryId
    {
        get => CountryId;
        set => CountryId = value;
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressStateId
    {
        get => StateId;
        set => StateId = value;
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressCityId
    {
        get => CityId;
        set => CityId = value;
    }

    // Contact & Branding
    public string? PrimaryContactName { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoFileUrl { get; set; }

    // Financial Settings (BaseCurrencyId and BooksStartDate are required)
    public Guid BaseCurrencyId { get; set; } // Required
    public Guid? ReportingCurrencyId { get; set; }
    public byte FiscalYearStartMonth { get; set; } = 4; // April
    public DateTime BooksStartDate { get; set; } = DateTime.UtcNow.Date; // Required - changed from nullable
    public bool EnableMultiCurrency { get; set; }
    public byte RoundingPrecision { get; set; } = 2;
    public RoundingMode? RoundingMode { get; set; }

    // System & Posting Controls
    public DateTime? AllowPostingFromDate { get; set; }
    public DateTime? AllowPostingToDate { get; set; }
    public bool LockBackDatedPosting { get; set; }
    public string? Notes { get; set; }
}

// Update Request
public class UpdateCompanyRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("legalName")]
    public string LegalName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("tradeName")]
    public string? TradeName { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("shortName")]
    public string? ShortName { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("legalStructure")]
    public LegalStructure LegalStructure { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("incorporationDate")]
    public DateTime? IncorporationDate { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("parentCompanyId")]
    public Guid? ParentCompanyId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public CompanyStatus Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("panNumber")]
    public string? PANNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("gstin")]
    public string? GSTIN { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("tanNumber")]
    public string? TANNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("otherTaxId")]
    public string? OtherTaxId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("registrationCountryId")]
    public Guid RegistrationCountryId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("registrationStateId")]
    public Guid? RegistrationStateId { get; set; }

    // Aliases for backward compatibility with Razor components (excluded from serialization)
    [System.Text.Json.Serialization.JsonIgnore]
    public string? TAN
    {
        get => TANNumber;
        set => TANNumber = value;
    }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string? OtherTaxIdentifier
    {
        get => OtherTaxId;
        set => OtherTaxId = value;
    }

    [System.Text.Json.Serialization.JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("cityId")]
    public Guid CityId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("stateId")]
    public Guid StateId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("countryId")]
    public Guid CountryId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("timeZoneId")]
    public Guid TimeZoneId { get; set; }

    // Address aliases for backward compatibility with Razor components (excluded from serialization)
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressCountryId
    {
        get => CountryId;
        set => CountryId = value;
    }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressStateId
    {
        get => StateId;
        set => StateId = value;
    }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid AddressCityId
    {
        get => CityId;
        set => CityId = value;
    }

    [System.Text.Json.Serialization.JsonPropertyName("primaryContactName")]
    public string? PrimaryContactName { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("primaryEmail")]
    public string? PrimaryEmail { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("primaryPhone")]
    public string? PrimaryPhone { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("logoFileUrl")]
    public string? LogoFileUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("baseCurrencyId")]
    public Guid BaseCurrencyId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("reportingCurrencyId")]
    public Guid? ReportingCurrencyId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("fiscalYearStartMonth")]
    public byte FiscalYearStartMonth { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("booksStartDate")]
    public DateTime BooksStartDate { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("enableMultiCurrency")]
    public bool EnableMultiCurrency { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("roundingPrecision")]
    public byte RoundingPrecision { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("roundingMode")]
    public RoundingMode? RoundingMode { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowPostingFromDate")]
    public DateTime? AllowPostingFromDate { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("allowPostingToDate")]
    public DateTime? AllowPostingToDate { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("lockBackDatedPosting")]
    public bool LockBackDatedPosting { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

// Lookup DTOs
public class CountryLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? PhoneCode { get; set; }
}

public class StateLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
}

public class CityLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid StateId { get; set; }
    public string? PostalCode { get; set; }
}

public class CurrencyLookupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string DisplayName => $"{Code} - {Name} ({Symbol})";
}

public class TimeZoneLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Offset { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsDefault { get; set; }
}

public class CompanyLookupDto
{
    public Guid Id { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName => $"{CompanyCode} - {LegalName}";
}
