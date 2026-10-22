namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

/// <summary>Administrative view of the address lookup values used by user forms.</summary>
public sealed class AddressLookupManagementViewModel
{
    public IReadOnlyList<AddressLookupProvince> Provinces { get; init; } = [];
    public IReadOnlyList<AddressLookupTownCity> TownCities { get; init; } = [];
    public IReadOnlyList<AddressLookupSuburb> Suburbs { get; init; } = [];
}

public sealed class AddressLookupProvince
{
    public int ProvinceId { get; init; }
    public string ProvinceName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class AddressLookupTownCity
{
    public int TownCityId { get; init; }
    public int ProvinceId { get; init; }
    public string ProvinceName { get; init; } = string.Empty;
    public string TownCityName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class AddressLookupSuburb
{
    public int SuburbId { get; init; }
    public int TownCityId { get; init; }
    public string ProvinceName { get; init; } = string.Empty;
    public string TownCityName { get; init; } = string.Empty;
    public string SuburbName { get; init; } = string.Empty;
    public string AreaCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class AddressLookupSave
{
    public string LookupType { get; init; } = string.Empty;
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? ProvinceId { get; init; }
    public int? TownCityId { get; init; }
    public string? AreaCode { get; init; }
}
