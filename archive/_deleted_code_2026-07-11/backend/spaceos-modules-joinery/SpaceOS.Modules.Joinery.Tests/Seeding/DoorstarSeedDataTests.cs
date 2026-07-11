using SpaceOS.Modules.Joinery.Infrastructure.Seeding;
using Xunit;

public class DoorstarSeedDataTests
{
    private static readonly string[] ValidComponentTypes =
        ["Frame", "Insert", "Clad", "FrameCore", "Blende", "Coating"];

    [Fact]
    public void AllPartDimensionRules_HaveValidComponentType()
    {
        Assert.All(DoorstarSeedData.PartDimensionRules,
            r => Assert.Contains(r.ComponentType, ValidComponentTypes));
    }

    [Fact]
    public void GlobalConstants_ContainRequiredKeys()
    {
        var required = new[] { "CuttingOversize", "CladdingOverhang", "MatyiWidth" };
        var keys = DoorstarSeedData.Constants.Select(c => c.Key).ToArray();
        Assert.All(required, k => Assert.Contains(k, keys));
    }

    [Fact]
    public void DoorTypeRules_NotEmpty()
    {
        Assert.NotEmpty(DoorstarSeedData.DoorTypeRules);
    }

    [Fact]
    public void ProcessTasks_NotEmpty()
    {
        Assert.NotEmpty(DoorstarSeedData.ProcessTasks);
    }

    [Fact]
    public void DoorTypeRules_AtLeast15()
    {
        Assert.True(DoorstarSeedData.DoorTypeRules.Count >= 15,
            $"Expected ≥15 DoorTypeRules, got {DoorstarSeedData.DoorTypeRules.Count}");
    }

    [Fact]
    public void ProcessTasks_AtLeast40()
    {
        Assert.True(DoorstarSeedData.ProcessTasks.Count >= 40,
            $"Expected ≥40 ProcessTaskTemplates, got {DoorstarSeedData.ProcessTasks.Count}");
    }

    [Fact]
    public void DoorTypeRules_ContainExtendedTypes()
    {
        var types = DoorstarSeedData.DoorTypeRules.Select(r => r.DoorType).ToHashSet();
        var expected = new[] { "Kétszárnyú", "Tűzálló", "Üveges", "Csúszó", "Redőnyes", "Akusztikus", "Forgóajtó" };
        Assert.All(expected, t => Assert.Contains(t, types));
    }

    [Fact]
    public void ProcessTasks_AllTaskIdsUnique()
    {
        var ids = DoorstarSeedData.ProcessTasks.Select(t => t.TaskId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ProcessTasks_ParentTaskIds_ReferenceExistingTasks()
    {
        var allIds = DoorstarSeedData.ProcessTasks.Select(t => t.TaskId).ToHashSet();
        var orphans = DoorstarSeedData.ProcessTasks
            .Where(t => t.ParentTaskId != null && !allIds.Contains(t.ParentTaskId!))
            .Select(t => t.TaskId)
            .ToList();
        Assert.Empty(orphans);
    }
}
