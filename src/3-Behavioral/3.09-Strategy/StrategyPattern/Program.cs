using StrategyPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

// Toronto landmarks
var unionStation   = new Location("Union Station",           43.6456, -79.3808);
var pearsonAirport = new Location("Pearson Airport",         43.6777, -79.6248);
var uOfT           = new Location("University of Toronto",   43.6629, -79.3957);
var scarborough    = new Location("Scarborough Town Centre",  43.7764, -79.2318);

Console.WriteLine("=== Strategy Pattern — Route Planner ===\n");

// Demo 1 — Same route, all four strategies back-to-back
Console.WriteLine("── DEMO 1: Union Station → Pearson Airport (all strategies) ──\n");
{
    var nav = new Navigator(new DrivingStrategy());
    nav.GetRoute(unionStation, pearsonAirport);
    Console.WriteLine();

    nav.SetStrategy(new PublicTransitStrategy());
    nav.GetRoute(unionStation, pearsonAirport);
    Console.WriteLine();

    nav.SetStrategy(new CyclingStrategy());
    nav.GetRoute(unionStation, pearsonAirport);
    Console.WriteLine();

    nav.SetStrategy(new WalkingStrategy());
    nav.GetRoute(unionStation, pearsonAirport);
}
Pause();

// Demo 2 — Runtime strategy swap based on context
Console.WriteLine("── DEMO 2: Runtime strategy swap — weekend plans ──\n");
{
    var nav = new Navigator(new DrivingStrategy());

    Console.WriteLine("  [Friday evening — drive home]");
    nav.GetRoute(uOfT, unionStation);
    Console.WriteLine();

    Console.WriteLine("  [Saturday — transit to avoid parking downtown]");
    nav.SetStrategy(new PublicTransitStrategy());
    nav.GetRoute(unionStation, scarborough);
    Console.WriteLine();

    Console.WriteLine("  [Sunday morning — cycling along the waterfront]");
    nav.SetStrategy(new CyclingStrategy());
    nav.GetRoute(unionStation, uOfT);
}
Pause();

// Demo 3 — Short trip: overhead of driving vs. walking
Console.WriteLine("── DEMO 3: Short trip — Union Station → U of T ──\n");
{
    var nav = new Navigator(new WalkingStrategy());
    Console.WriteLine("  [Walking — practical for short distances]");
    nav.GetRoute(unionStation, uOfT);
    Console.WriteLine();

    nav.SetStrategy(new DrivingStrategy());
    Console.WriteLine("  [Driving — overhead of highway detour outweighs speed]");
    nav.GetRoute(unionStation, uOfT);
}
Pause();

// Demo 4 — Long trip comparison summary
Console.WriteLine("── DEMO 4: Long trip — Union Station → Scarborough (side by side) ──\n");
{
    IRouteStrategy[] strategies =
    [
        new DrivingStrategy(),
        new PublicTransitStrategy(),
        new CyclingStrategy(),
        new WalkingStrategy(),
    ];

    var origin = unionStation;
    var dest   = scarborough;

    Console.WriteLine($"  {"Strategy",-16} {"Distance",10} {"Duration",10} {"Cost",8}");
    Console.WriteLine($"  {new string('-', 48)}");

    foreach (var strategy in strategies)
    {
        var r = strategy.Calculate(origin, dest);
        var dur = r.Duration.TotalHours >= 1
            ? $"{(int)r.Duration.TotalHours}h {r.Duration.Minutes:D2}m"
            : $"{r.Duration.Minutes}m";
        var cost = r.Cost == 0 ? "Free" : $"${r.Cost:F2}";
        Console.WriteLine($"  {r.Mode,-16} {r.DistanceKm,8:F1} km {dur,8} {cost,8}");
    }
}

Console.WriteLine("\n=== End of Strategy pattern demo ===");
