using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

/// <summary>Calculated presentation data for the completed-tests management report.</summary>
public static class TestReportInsights
{
    public sealed record DailyVolume(DateTime Date, int TestsCompleted);
    public sealed record ConsumableUsage(string ConsumableName, int EstimatedUnitsUsed, int TestsContributing);

    public static IReadOnlyList<DailyVolume> DailyVolumes(IEnumerable<TestsPerformedDetail> tests) =>
        tests.Where(test => test.CompletionDateTime.HasValue)
            .GroupBy(test => test.CompletionDateTime!.Value.Date)
            .OrderBy(group => group.Key)
            .Select(group => new DailyVolume(group.Key, group.Count()))
            .ToList();

    public static IReadOnlyList<ConsumableUsage> EstimatedConsumableUsage(
        IEnumerable<TestsPerformedDetail> tests, IEnumerable<TestType> testTypes)
    {
        var typeById = testTypes.ToDictionary(type => type.TestTypeId);
        var typeByName = testTypes
            .GroupBy(type => $"{type.TestCategory?.CategoryName}|{type.TestName}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var usage = new Dictionary<int, (string Name, int Units, int Tests)>();
        foreach (var test in tests)
        {
            TestType? type = null;
            if (test.TestTypeId > 0)
                typeById.TryGetValue(test.TestTypeId, out type);
            type ??= typeByName.GetValueOrDefault($"{test.CategoryName}|{test.TestName}");
            if (type is null)
                continue;

            foreach (var requirement in type.TestTypeConsumable)
            {
                if (requirement.ConsumableId <= 0 || requirement.QuantityRequired <= 0)
                    continue;
                var name = requirement.Consumable?.Name ?? "Unknown consumable";
                if (usage.TryGetValue(requirement.ConsumableId, out var current))
                    usage[requirement.ConsumableId] = (current.Name, current.Units + requirement.QuantityRequired, current.Tests + 1);
                else
                    usage[requirement.ConsumableId] = (name, requirement.QuantityRequired, 1);
            }
        }

        return usage.Values
            .Select(value => new ConsumableUsage(value.Name, value.Units, value.Tests))
            .OrderByDescending(value => value.EstimatedUnitsUsed)
            .ThenBy(value => value.ConsumableName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
