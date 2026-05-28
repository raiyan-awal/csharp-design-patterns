namespace RepositoryPattern;

/// <summary>
/// DOMAIN ENTITY - Product
///
/// This is a domain entity that represents a product in our system.
/// The Repository pattern helps us abstract away how we store and retrieve these entities.
///
/// KEY POINTS:
/// - Entities have unique identities (Id property)
/// - Entities encapsulate business logic and state
/// - Entities should be persistence-ignorant (no database attributes here!)
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier for the product.
    /// In a real application, this might be a Guid or auto-increment integer.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product category (e.g., "Electronics", "Clothing", "Books").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Product price in USD.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Number of units in stock.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Whether the product is currently active/available for sale.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Override ToString for easy display.
    /// </summary>
    public override string ToString()
    {
        return $"[{Id}] {Name} | ${Price:F2} | Stock: {StockQuantity} | {Category}";
    }
}
