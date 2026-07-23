namespace AdaptiveTeamBuilder.Data.Enums;

public static class PositionTypes
{
    public const string Developer = "Developer";
    public const string UxDesigner = "UxDesigner";
    public const string Product = "Product";
    public const string QualityAssurance = "QualityAssurance";

    public static readonly string[] All =
    [
        Developer,
        UxDesigner,
        Product,
        QualityAssurance,
    ];

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && All.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return All.FirstOrDefault(p => p.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public static class ExperienceLevels
{
    public const string Junior = "Junior";
    public const string Mid = "Mid";
    public const string Senior = "Senior";
    public const string Lead = "Lead";
    public const string Architect = "Architect";

    public static readonly string[] All =
    [
        Junior,
        Mid,
        Senior,
        Lead,
        Architect,
    ];
}

public static class ProductRoleSpecialties
{
    public const string ScrumMaster = "ScrumMaster";
    public const string BusinessAnalyst = "BusinessAnalyst";
    public const string ProductOwner = "ProductOwner";

    public static readonly string[] All =
    [
        ScrumMaster,
        BusinessAnalyst,
        ProductOwner,
    ];
}
