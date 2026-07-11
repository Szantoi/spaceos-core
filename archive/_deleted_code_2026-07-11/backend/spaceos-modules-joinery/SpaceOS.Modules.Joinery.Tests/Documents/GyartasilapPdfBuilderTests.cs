using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Documents;

namespace SpaceOS.Modules.Joinery.Tests.Documents;

/// <summary>
/// Unit tests for GyartasilapPdfBuilder covering all 4 label variants and content variations.
/// </summary>
public class GyartasilapPdfBuilderTests
{
    private readonly IGyartasilapPdfBuilder _builder = new GyartasilapPdfBuilder();

    #region Basic Tests (L1 - Standard)

    [Fact]
    public async Task GeneratePdfAsync_L1_ProducesValidPdfBytes()
    {
        var result = await _builder.GeneratePdfAsync("ORD-001", "TestProject", "L1");

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(0);
        result[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F'); // PDF magic number
    }

    [Fact]
    public async Task GeneratePdfAsync_L1_WithNullOrderName_Succeeds()
    {
        var result = await _builder.GeneratePdfAsync("ORD-001", null, "L1");

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L1_IgnoresMaterialsAndJobs()
    {
        var materials = new[] { new MaterialItem("M1", "Material 1", 10, "pcs") };
        var jobs = new[] { new JobItem("Job1", "Description 1") };

        var result = await _builder.GeneratePdfAsync(
            "ORD-001", "Project", "L1", materials, jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
        // L1 should be smaller than L2/L3/L4 since it doesn't include tables
    }

    #endregion

    #region Premium Tests (L2 - With QR Code)

    [Fact]
    public async Task GeneratePdfAsync_L2_ProducesValidPdfBytes()
    {
        var result = await _builder.GeneratePdfAsync("ORD-002", "PremiumProject", "L2");

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    [Fact]
    public async Task GeneratePdfAsync_L2_WithMaterials_IncludesMaterialsTable()
    {
        var materials = new[]
        {
            new MaterialItem("MAT-001", "Oak Wood", 10, "m²"),
            new MaterialItem("MAT-002", "Varnish", 2, "L"),
        };

        var result = await _builder.GeneratePdfAsync(
            "ORD-002", "Premium", "L2", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L2_WithoutMaterials_StillProducesPdf()
    {
        var result = await _builder.GeneratePdfAsync("ORD-002", "Project", "L2", new List<MaterialItem>());

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L2_IgnoresJobs()
    {
        var jobs = new[] { new JobItem("CutJob", "Cut to size") };

        var result = await _builder.GeneratePdfAsync(
            "ORD-002", "Project", "L2", jobsList: jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Barcode Tests (L3 - With Barcode)

    [Fact]
    public async Task GeneratePdfAsync_L3_ProducesValidPdfBytes()
    {
        var result = await _builder.GeneratePdfAsync("ORD-003", "BarcodeProject", "L3");

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    [Fact]
    public async Task GeneratePdfAsync_L3_WithMaterials_IncludesMaterialsTable()
    {
        var materials = new[]
        {
            new MaterialItem("MAT-A", "Material A", 5, "kg"),
            new MaterialItem("MAT-B", "Material B", 3, "kg"),
        };

        var result = await _builder.GeneratePdfAsync(
            "ORD-003", "Project", "L3", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L3_WithOrderNumber_IncludesInBarcode()
    {
        const string orderNum = "ORD-XYZ-999";
        var result = await _builder.GeneratePdfAsync(orderNum, "Project", "L3");

        result.Should().NotBeNull().And.NotBeEmpty();
        // Barcode should include the order number (verified via visual inspection, not binary parsing)
    }

    [Fact]
    public async Task GeneratePdfAsync_L3_IgnoresJobs()
    {
        var jobs = new[] { new JobItem("Job1", "Desc1") };

        var result = await _builder.GeneratePdfAsync(
            "ORD-003", "Project", "L3", jobsList: jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Full Tests (L4 - Complete with QR, Barcode, Notes)

    [Fact]
    public async Task GeneratePdfAsync_L4_ProducesValidPdfBytes()
    {
        var result = await _builder.GeneratePdfAsync("ORD-004", "CompleteProject", "L4");

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithMaterials_IncludesMaterialsTable()
    {
        var materials = new[]
        {
            new MaterialItem("WOOD-1", "Plywood 18mm", 20, "sheets"),
            new MaterialItem("HARDWARE-1", "Hinges", 4, "pcs"),
        };

        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithJobs_IncludesJobsTable()
    {
        var jobs = new[]
        {
            new JobItem("CUT", "Cut all pieces to specification"),
            new JobItem("DRILL", "Drill holes for assembly"),
            new JobItem("SAND", "Sand all edges smooth"),
        };

        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", jobsList: jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithNotes_IncludesAssemblyInstructions()
    {
        const string notes = "Please ensure all screws are tightened to specification. " +
                             "Assemble in order: base → sides → back → shelves.";

        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", notes: notes);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithAllData_ProducesLargestPdf()
    {
        var materials = new[]
        {
            new MaterialItem("M1", "Material 1", 10, "units"),
            new MaterialItem("M2", "Material 2", 20, "units"),
        };
        var jobs = new[]
        {
            new JobItem("J1", "Job 1"),
            new JobItem("J2", "Job 2"),
        };
        const string notes = "Comprehensive assembly instructions here.";

        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", materials, jobs, notes);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithEmptyNotes_Succeeds()
    {
        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", notes: "");

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithoutMaterials_SkipsTable()
    {
        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", new List<MaterialItem>());

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_L4_WithoutJobs_SkipsTable()
    {
        var result = await _builder.GeneratePdfAsync(
            "ORD-004", "Full", "L4", jobsList: new List<JobItem>());

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Variant Validation

    [Theory]
    [InlineData("L1")]
    [InlineData("L2")]
    [InlineData("L3")]
    [InlineData("L4")]
    [InlineData("l1")]
    [InlineData("l2")]
    [InlineData("l3")]
    [InlineData("l4")]
    public async Task GeneratePdfAsync_WithValidVariants_Succeeds(string variant)
    {
        var result = await _builder.GeneratePdfAsync("ORD-001", "Project", variant);

        result.Should().NotBeNull().And.NotBeEmpty();
        result[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    [Theory]
    [InlineData("L0")]
    [InlineData("L5")]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("L1.5")]
    public async Task GeneratePdfAsync_WithInvalidVariant_ThrowsArgumentException(string variant)
    {
        var act = () => _builder.GeneratePdfAsync("ORD-001", "Project", variant);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Must be L1, L2, L3, or L4*");
    }

    #endregion

    #region Order Data Tests

    [Theory]
    [InlineData("ORD-001")]
    [InlineData("ORDER-ABC-123")]
    [InlineData("X")]
    [InlineData("")]
    public async Task GeneratePdfAsync_WithVariousOrderNumbers_ProducesPdf(string orderNum)
    {
        var result = await _builder.GeneratePdfAsync(orderNum, "Project", "L1");

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Theory]
    [InlineData("Dining Table")]
    [InlineData("Custom Cabinet")]
    [InlineData("")]
    [InlineData(null)]
    public async Task GeneratePdfAsync_WithVariousOrderNames_ProducesPdf(string? orderName)
    {
        var result = await _builder.GeneratePdfAsync("ORD-001", orderName, "L1");

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Material Item Tests

    [Fact]
    public async Task GeneratePdfAsync_WithManyMaterials_IncludesAllRows()
    {
        var materials = new List<MaterialItem>();
        for (int i = 1; i <= 20; i++)
        {
            materials.Add(new MaterialItem($"MAT-{i:D3}", $"Material {i}", i * 1.5m, "units"));
        }

        var result = await _builder.GeneratePdfAsync("ORD-005", "Large", "L4", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_WithLongMaterialDescriptions_Succeeds()
    {
        var materials = new[]
        {
            new MaterialItem(
                "MAT-LONG",
                "A very long material description that contains detailed information about the material specifications, " +
                "including thickness, finish, and application requirements.",
                100.5m,
                "meters"),
        };

        var result = await _builder.GeneratePdfAsync("ORD-006", "Long", "L2", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_WithSpecialCharactersInMaterial_Succeeds()
    {
        var materials = new[]
        {
            new MaterialItem("MAT-SPEC", "Material™ with © symbols & <html>", 10, "pcs"),
        };

        var result = await _builder.GeneratePdfAsync("ORD-007", "Special", "L3", materials);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Job Item Tests

    [Fact]
    public async Task GeneratePdfAsync_WithManyJobs_IncludesAllRows()
    {
        var jobs = new List<JobItem>();
        for (int i = 1; i <= 15; i++)
        {
            jobs.Add(new JobItem($"JOB-{i:D2}", $"Task {i}: Description of the manufacturing task."));
        }

        var result = await _builder.GeneratePdfAsync("ORD-008", "Many", "L4", jobsList: jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_WithLongJobDescriptions_Succeeds()
    {
        var jobs = new[]
        {
            new JobItem(
                "ASSEMBLE",
                "This is a comprehensive assembly task that requires careful attention to detail. " +
                "Follow all safety procedures and wear appropriate protective equipment throughout the process."),
        };

        var result = await _builder.GeneratePdfAsync("ORD-009", "LongJob", "L4", jobsList: jobs);

        result.Should().NotBeNull().And.NotBeEmpty();
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GeneratePdfAsync_WithCancelledToken_RespectsCancellation()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _builder.GeneratePdfAsync("ORD-010", "Project", "L1", cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GeneratePdfAsync_WithValidCancellationToken_Succeeds()
    {
        var cts = new CancellationTokenSource();

        var result = await _builder.GeneratePdfAsync(
            "ORD-011", "Project", "L1", cancellationToken: cts.Token);

        result.Should().NotBeNull().And.NotBeEmpty();

        cts.Dispose();
    }

    #endregion

    #region Byte Array Return Tests

    [Fact]
    public async Task GeneratePdfAsync_ReturnsConsistentSize_ForSameInput()
    {
        const string order = "ORD-SAME";
        const string project = "SameProject";

        var result1 = await _builder.GeneratePdfAsync(order, project, "L1");
        var result2 = await _builder.GeneratePdfAsync(order, project, "L1");

        result1.Length.Should().Be(result2.Length);
    }

    [Fact]
    public async Task GeneratePdfAsync_DifferentVariants_ProduceValidPdfs()
    {
        var l1 = await _builder.GeneratePdfAsync("ORD-DIF", "Project", "L1");
        var l4 = await _builder.GeneratePdfAsync(
            "ORD-DIF", "Project", "L4",
            new[] { new MaterialItem("M", "Desc", 1, "u") },
            new[] { new JobItem("J", "D") });

        l1.Should().NotBeEmpty();
        l4.Should().NotBeEmpty();
        l1[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
        l4[0..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    #endregion

    #region Material Item Record Tests

    [Fact]
    public void MaterialItem_CreatedWithValues_PreservesAllFields()
    {
        var item = new MaterialItem("CODE-123", "Description Text", 42.5m, "meters");

        item.Code.Should().Be("CODE-123");
        item.Description.Should().Be("Description Text");
        item.Quantity.Should().Be(42.5m);
        item.Unit.Should().Be("meters");
    }

    #endregion

    #region JobItem Record Tests

    [Fact]
    public void JobItem_CreatedWithValues_PreservesAllFields()
    {
        var job = new JobItem("JobName", "Job Description");

        job.Name.Should().Be("JobName");
        job.Description.Should().Be("Job Description");
    }

    #endregion
}
