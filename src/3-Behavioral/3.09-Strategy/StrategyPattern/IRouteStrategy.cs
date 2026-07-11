namespace StrategyPattern;

public interface IRouteStrategy
{
    string Name { get; }
    Route Calculate(Location origin, Location destination);
}
