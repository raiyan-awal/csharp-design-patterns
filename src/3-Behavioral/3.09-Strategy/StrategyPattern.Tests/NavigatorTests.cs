using StrategyPattern;

namespace StrategyPattern.Tests;

public class NavigatorTests
{
    // Two Toronto landmarks ~2 km apart (Union Station → U of T)
    private static readonly Location UnionStation = new("Union Station",          43.6456, -79.3808);
    private static readonly Location UofT         = new("University of Toronto",  43.6629, -79.3957);

    // Far apart (~24 km straight line: Union Station → Pearson)
    private static readonly Location Pearson      = new("Pearson Airport",        43.6777, -79.6248);

    // ── Navigator ────────────────────────────────────────────────────────────

    [Fact]
    public void Navigator_UsesAssignedStrategy_ReturnsMatchingMode()
    {
        var nav   = new Navigator(new DrivingStrategy());
        var route = nav.GetRoute(UnionStation, UofT);
        Assert.Equal("Driving", route.Mode);
    }

    [Fact]
    public void Navigator_CurrentStrategy_ReflectsActiveStrategy()
    {
        var nav = new Navigator(new WalkingStrategy());
        Assert.Equal("Walking", nav.CurrentStrategy);
    }

    [Fact]
    public void SetStrategy_SwapsAlgorithmAtRuntime()
    {
        var nav = new Navigator(new DrivingStrategy());
        nav.SetStrategy(new WalkingStrategy());

        var route = nav.GetRoute(UnionStation, UofT);
        Assert.Equal("Walking", route.Mode);
        Assert.Equal("Walking", nav.CurrentStrategy);
    }

    // ── Driving ──────────────────────────────────────────────────────────────

    [Fact]
    public void DrivingStrategy_HasPositiveCost()
    {
        var route = new DrivingStrategy().Calculate(UnionStation, Pearson);
        Assert.True(route.Cost > 0);
    }

    [Fact]
    public void DrivingStrategy_HasLongerDistanceThanWalkingForSameRoute()
    {
        // Driving uses a higher path multiplier (highway detour) than walking
        var driving = new DrivingStrategy().Calculate(UnionStation, Pearson);
        var walking = new WalkingStrategy().Calculate(UnionStation, Pearson);
        Assert.True(driving.DistanceKm > walking.DistanceKm);
    }

    [Fact]
    public void DrivingStrategy_IsFasterThanWalkingForSameRoute()
    {
        var driving = new DrivingStrategy().Calculate(UnionStation, Pearson);
        var walking = new WalkingStrategy().Calculate(UnionStation, Pearson);
        Assert.True(driving.Duration < walking.Duration);
    }

    [Fact]
    public void DrivingStrategy_CostScalesWithPathMultiplier()
    {
        var cheap    = new DrivingStrategy(pathMultiplier: 1.0, costPerKm: 0.10m);
        var expensive = new DrivingStrategy(pathMultiplier: 2.0, costPerKm: 0.10m);

        var cheapRoute     = cheap.Calculate(UnionStation, Pearson);
        var expensiveRoute = expensive.Calculate(UnionStation, Pearson);

        Assert.True(expensiveRoute.Cost > cheapRoute.Cost);
    }

    // ── Walking ──────────────────────────────────────────────────────────────

    [Fact]
    public void WalkingStrategy_CostIsZero()
    {
        var route = new WalkingStrategy().Calculate(UnionStation, Pearson);
        Assert.Equal(0m, route.Cost);
    }

    [Fact]
    public void WalkingStrategy_SlowerThanCycling()
    {
        var walking = new WalkingStrategy().Calculate(UnionStation, Pearson);
        var cycling = new CyclingStrategy().Calculate(UnionStation, Pearson);
        Assert.True(walking.Duration > cycling.Duration);
    }

    // ── Cycling ──────────────────────────────────────────────────────────────

    [Fact]
    public void CyclingStrategy_CostIsZero()
    {
        var route = new CyclingStrategy().Calculate(UnionStation, Pearson);
        Assert.Equal(0m, route.Cost);
    }

    [Fact]
    public void CyclingStrategy_FasterThanWalkingSlowerThanDriving()
    {
        var walking = new WalkingStrategy().Calculate(UnionStation, Pearson);
        var cycling = new CyclingStrategy().Calculate(UnionStation, Pearson);
        var driving = new DrivingStrategy().Calculate(UnionStation, Pearson);

        Assert.True(cycling.Duration < walking.Duration);
        Assert.True(cycling.Duration > driving.Duration);
    }

    // ── Public Transit ───────────────────────────────────────────────────────

    [Fact]
    public void PublicTransitStrategy_CostIsFlatFare()
    {
        var flatFare = 3.30m;
        var strategy = new PublicTransitStrategy(flatFare: flatFare);
        var route    = strategy.Calculate(UnionStation, Pearson);
        Assert.Equal(flatFare, route.Cost);
    }

    [Fact]
    public void PublicTransitStrategy_FlatFareDoesNotScaleWithDistance()
    {
        var strategy = new PublicTransitStrategy(flatFare: 3.30m);

        var shortRoute = strategy.Calculate(UnionStation, UofT);
        var longRoute  = strategy.Calculate(UnionStation, Pearson);

        Assert.Equal(shortRoute.Cost, longRoute.Cost);
    }

    // ── Steps ────────────────────────────────────────────────────────────────

    [Fact]
    public void AllStrategies_ReturnAtLeastOneStep()
    {
        IRouteStrategy[] strategies =
        [
            new DrivingStrategy(),
            new WalkingStrategy(),
            new CyclingStrategy(),
            new PublicTransitStrategy(),
        ];

        foreach (var s in strategies)
        {
            var route = s.Calculate(UnionStation, UofT);
            Assert.NotEmpty(route.Steps);
        }
    }

    // ── Same location ────────────────────────────────────────────────────────

    [Fact]
    public void SameOriginAndDestination_ReturnsZeroDistance()
    {
        var route = new DrivingStrategy().Calculate(UnionStation, UnionStation);
        Assert.Equal(0.0, route.DistanceKm);
    }

    [Fact]
    public void SameOriginAndDestination_WalkingCostRemainsZero()
    {
        var route = new WalkingStrategy().Calculate(UnionStation, UnionStation);
        Assert.Equal(0m, route.Cost);
    }
}
