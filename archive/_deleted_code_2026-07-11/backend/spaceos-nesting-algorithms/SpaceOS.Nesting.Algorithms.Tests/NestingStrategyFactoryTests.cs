using FluentAssertions;
using SpaceOS.Nesting.Algorithms.Strategies;
using Xunit;

namespace SpaceOS.Nesting.Algorithms.Tests;

public class NestingStrategyFactoryTests
{
    private static NestingStrategyFactory BuildFactory()
        => new(new INestingStrategy[]
        {
            new FfdhNestingStrategy(),
            new GuillotineNestingStrategy(),
            new MaxRectsNestingStrategy()
        });

    // ── Strategy lookup ───────────────────────────────────────────────────────

    [Fact]
    public void GetStrategy_Ffdh_ReturnsCorrectType()
    {
        var factory = BuildFactory();

        factory.GetStrategy("FFDH").Should().BeOfType<FfdhNestingStrategy>();
    }

    [Fact]
    public void GetStrategy_Guillotine_ReturnsCorrectType()
    {
        var factory = BuildFactory();

        factory.GetStrategy("Guillotine").Should().BeOfType<GuillotineNestingStrategy>();
    }

    [Fact]
    public void GetStrategy_MaxRects_ReturnsCorrectType()
    {
        var factory = BuildFactory();

        factory.GetStrategy("MaxRects").Should().BeOfType<MaxRectsNestingStrategy>();
    }

    // ── Case-insensitive lookup ───────────────────────────────────────────────

    [Theory]
    [InlineData("ffdh")]
    [InlineData("FFDH")]
    [InlineData("Ffdh")]
    public void GetStrategy_CaseInsensitive_Works(string name)
    {
        var factory = BuildFactory();

        var act = () => factory.GetStrategy(name);

        act.Should().NotThrow();
        factory.GetStrategy(name).Should().BeOfType<FfdhNestingStrategy>();
    }

    // ── Unknown strategy ──────────────────────────────────────────────────────

    [Fact]
    public void GetStrategy_UnknownName_ThrowsKeyNotFoundException()
    {
        var factory = BuildFactory();

        var act = () => factory.GetStrategy("Unknown");

        act.Should().Throw<KeyNotFoundException>().WithMessage("*Unknown*");
    }

    // ── AvailableStrategies ───────────────────────────────────────────────────

    [Fact]
    public void AvailableStrategies_ContainsAllThree()
    {
        var factory = BuildFactory();

        factory.AvailableStrategies.Should().HaveCount(3);
    }
}
