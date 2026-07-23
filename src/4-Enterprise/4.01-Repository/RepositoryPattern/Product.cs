namespace RepositoryPattern;

// Domain entity — persistence-ignorant (no database attributes, no ORM concerns).
// Id is assigned by the repository on first Add; all other properties are mutable
// to support UpdateAsync patching them in place.
public sealed class Product
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = "";
    public string  Category      { get; set; } = "";
    public decimal Price         { get; set; }
    public int     StockQuantity { get; set; }
    public bool    IsActive      { get; set; } = true;

    public override string ToString() =>
        $"[{Id}] {Name,-36} ${Price,8:F2}  Stock: {StockQuantity,3}  {Category}";
}
