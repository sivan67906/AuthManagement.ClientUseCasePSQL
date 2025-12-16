using System.Net.Http.Json;
using System.Net.Http.Headers;
using AuthManagement.Models;

namespace AuthManagement.Services;

public class CompanyService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public CompanyService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        var token = await _authStateProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Company CRUD Operations
    public async Task<ApiResponse<List<CompanyDto>>> GetAllCompaniesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<CompanyDto>>>("api/company")
               ?? new ApiResponse<List<CompanyDto>> { Success = false, Data = new List<CompanyDto>() };
    }

    public async Task<ApiResponse<CompanyDto>> GetCompanyAsync(Guid id)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<CompanyDto>>($"api/company/{id}")
               ?? new ApiResponse<CompanyDto> { Success = false };
    }

    public async Task<ApiResponse<CompanyDto>> CreateCompanyAsync(CreateCompanyRequest request)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("api/company", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>()
                       ?? new ApiResponse<CompanyDto> { Success = false, Message = "Failed to parse response" };
            }
            else
            {
                // Try to parse as ApiResponse first
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
                    if (errorResponse != null)
                    {
                        return errorResponse;
                    }
                }
                catch
                {
                    // If parsing fails, use raw content
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ApiResponse<CompanyDto> 
                { 
                    Success = false, 
                    Message = $"Failed to create company. Server returned {response.StatusCode}.", 
                    Errors = new List<string> { errorContent }
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<CompanyDto> 
            { 
                Success = false, 
                Message = "An error occurred while creating company", 
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<CompanyDto>> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync($"api/company/{id}", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>()
                       ?? new ApiResponse<CompanyDto> { Success = false, Message = "Failed to parse response" };
            }
            else
            {
                // Try to parse as ApiResponse first
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
                    if (errorResponse != null)
                    {
                        return errorResponse;
                    }
                }
                catch
                {
                    // If parsing fails, use raw content
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ApiResponse<CompanyDto> 
                { 
                    Success = false, 
                    Message = $"Failed to update company. Server returned {response.StatusCode}.", 
                    Errors = new List<string> { errorContent }
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<CompanyDto> 
            { 
                Success = false, 
                Message = "An error occurred while updating company", 
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteCompanyAsync(Guid id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"api/company/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
               ?? new ApiResponse<bool> { Success = false };
    }

    // Lookup Operations
    public async Task<ApiResponse<List<CountryLookupDto>>> GetCountriesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<CountryLookupDto>>>("api/company/lookups/countries")
               ?? new ApiResponse<List<CountryLookupDto>> { Success = false, Data = new List<CountryLookupDto>() };
    }

    public async Task<ApiResponse<List<StateLookupDto>>> GetStatesByCountryAsync(Guid countryId)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<StateLookupDto>>>($"api/company/lookups/states/{countryId}")
               ?? new ApiResponse<List<StateLookupDto>> { Success = false, Data = new List<StateLookupDto>() };
    }

    public async Task<ApiResponse<List<CityLookupDto>>> GetCitiesByStateAsync(Guid stateId)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<CityLookupDto>>>($"api/company/lookups/cities/{stateId}")
               ?? new ApiResponse<List<CityLookupDto>> { Success = false, Data = new List<CityLookupDto>() };
    }

    public async Task<ApiResponse<List<CurrencyLookupDto>>> GetCurrenciesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<CurrencyLookupDto>>>("api/company/lookups/currencies")
               ?? new ApiResponse<List<CurrencyLookupDto>> { Success = false, Data = new List<CurrencyLookupDto>() };
    }

    public async Task<ApiResponse<List<TimeZoneLookupDto>>> GetTimeZonesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<TimeZoneLookupDto>>>("api/company/lookups/timezones")
               ?? new ApiResponse<List<TimeZoneLookupDto>> { Success = false, Data = new List<TimeZoneLookupDto>() };
    }

    public async Task<ApiResponse<List<TimeZoneLookupDto>>> GetTimezonesByCountryAsync(Guid countryId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync($"api/company/timezones-by-country/{countryId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TimeZoneLookupDto>>>();
                return result ?? new ApiResponse<List<TimeZoneLookupDto>>
                {
                    Success = false,
                    Message = "Invalid response format",
                    Data = new List<TimeZoneLookupDto>()
                };
            }
            
            return new ApiResponse<List<TimeZoneLookupDto>>
            {
                Success = false,
                Message = $"Error: {response.StatusCode}",
                Data = new List<TimeZoneLookupDto>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting timezones by country: {ex.Message}");
            return new ApiResponse<List<TimeZoneLookupDto>>
            {
                Success = false,
                Message = "Network error occurred",
                Data = new List<TimeZoneLookupDto>()
            };
        }
    }

    public async Task<ApiResponse<List<CompanyLookupDto>>> GetCompaniesLookupAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<ApiResponse<List<CompanyLookupDto>>>("api/company/lookups/companies")
               ?? new ApiResponse<List<CompanyLookupDto>> { Success = false, Data = new List<CompanyLookupDto>() };
    }
}
