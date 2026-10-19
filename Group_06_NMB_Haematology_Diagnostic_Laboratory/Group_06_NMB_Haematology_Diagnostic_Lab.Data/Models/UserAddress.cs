namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

// Dapper model for the additive structured-address tables.
public class UserAddress
{
    public int UserId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public int SuburbId { get; set; }
    public int TownCityId { get; set; }
    public int ProvinceId { get; set; }
    public string? SuburbName { get; set; }
    public string? AreaCode { get; set; }
    public string? TownCityName { get; set; }
    public string? ProvinceName { get; set; }
}

public class Province
{
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
}

public class TownCity
{
    public int TownCityId { get; set; }
    public int ProvinceId { get; set; }
    public string TownCityName { get; set; } = string.Empty;
}

public class Suburb
{
    public int SuburbId { get; set; }
    public int TownCityId { get; set; }
    public string SuburbName { get; set; } = string.Empty;
    public string AreaCode { get; set; } = string.Empty;
}
