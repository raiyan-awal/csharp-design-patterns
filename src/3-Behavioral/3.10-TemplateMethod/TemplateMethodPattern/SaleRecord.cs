namespace TemplateMethodPattern;

public record SaleRecord(string Product, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}
