namespace SpecificationPattern;

public sealed class Product
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = "";
    public string  Category      { get; set; } = "";
    public decimal Price         { get; set; }
    public int     StockQuantity { get; set; }
    public bool    IsActive      { get; set; } = true;
    public double  Rating        { get; set; }  // 0.0 – 5.0
    public string  Brand         { get; set; } = "";

    public override string ToString()
        => $"[{Id:D3}] {Name,-36} ${Price,8:F2}  {Category,-14} stock:{StockQuantity,4}  ★{Rating:F1}";
}
