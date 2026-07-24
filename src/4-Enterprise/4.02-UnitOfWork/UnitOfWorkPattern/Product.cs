namespace UnitOfWorkPattern;

// Persistence-ignorant domain entity. Mutable so a repository can patch it in place.
public sealed class Product
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = "";
    public decimal Price         { get; set; }
    public int     StockQuantity { get; set; }

    // Deep-enough copy so that a caller mutating the returned instance can
    // never corrupt the store before the Unit of Work decides to commit.
    public Product Clone() => new() { Id = Id, Name = Name, Price = Price, StockQuantity = StockQuantity };

    public override string ToString() =>
        $"[{Id}] {Name,-24} ${Price,8:F2}  Stock: {StockQuantity,3}";
}
