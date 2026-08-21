using ValueObjectPattern.Values;

namespace ValueObjectPattern.Domain;

public sealed class PropertyListing
{
    public int Id { get; }
    public string Title { get; }
    public Address Location { get; }
    public Money AskingPrice { get; }
    public DateRange Availability { get; }

    public PropertyListing(int id, string title, Address location, Money askingPrice,
                           DateRange availability)
    {
        Id           = id;
        Title        = title;
        Location     = location;
        AskingPrice  = askingPrice;
        Availability = availability;
    }

    // Returns a new listing with a different price; all other fields stay the same.
    public PropertyListing WithPrice(Money newPrice) =>
        new(Id, Title, Location, newPrice, Availability);

    // Returns a new listing with a different availability window.
    public PropertyListing WithAvailability(DateRange newAvailability) =>
        new(Id, Title, Location, AskingPrice, newAvailability);

    public override string ToString() =>
        $"[{Id}] {Title}\n    {Location}\n    {AskingPrice}\n    Available: {Availability}";
}
