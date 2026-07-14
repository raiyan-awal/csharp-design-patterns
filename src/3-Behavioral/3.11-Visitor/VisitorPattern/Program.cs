using VisitorPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

IReadOnlyList<ICartItem> cart =
[
    new PhysicalProduct("Laptop",              1_299.99m, weightKg: 2.1),
    new PhysicalProduct("Mechanical Keyboard",   149.99m, weightKg: 1.2),
    new DigitalProduct ("Adobe CC License",      599.99m),
    new FoodItem       ("Organic Apples",          8.99m),
    new FoodItem       ("Atlantic Salmon Fillet",  24.99m, isRefrigerated: true),
    new SubscriptionService("Amazon Prime",        9.99m, months: 12),
];

Console.WriteLine("=== Visitor Pattern — Shopping Cart Checkout ===\n");

// Demo 1 — Receipt: same operation, each item formats itself differently
Console.WriteLine("── DEMO 1: Receipt ──\n");
{
    var receipt = new ReceiptVisitor();
    foreach (var item in cart)
        item.Accept(receipt);

    Console.WriteLine($"  {"Item",-32} {"Price",9}  Notes");
    Console.WriteLine($"  {new string('-', 58)}");
    receipt.Print();
    Console.WriteLine($"  {new string('-', 58)}");
    Console.WriteLine($"  {"Subtotal",-32} ${cart.Sum(i => i.Price),8:F2}");
}
Pause();

// Demo 2 — Tax: each item type has its own tax rule
Console.WriteLine("── DEMO 2: Tax Calculation ──\n");
{
    var tax = new TaxVisitor();
    foreach (var item in cart)
        item.Accept(tax);
    Console.WriteLine($"\n  Total tax: ${tax.TotalTax:F2}");
}
Pause();

// Demo 3 — Shipping: physical and refrigerated food cost money; digital is free
Console.WriteLine("── DEMO 3: Shipping Costs ──\n");
{
    var shipping = new ShippingVisitor();
    foreach (var item in cart)
        item.Accept(shipping);
    Console.WriteLine($"\n  Total shipping: ${shipping.TotalShipping:F2}");
}
Pause();

// Demo 4 — Full checkout summary
Console.WriteLine("── DEMO 4: Full Checkout Summary ──\n");
{
    var receipt  = new ReceiptVisitor();
    var tax      = new TaxVisitor();
    var shipping = new ShippingVisitor();

    foreach (var item in cart)
    {
        item.Accept(receipt);
        item.Accept(tax);
        item.Accept(shipping);
    }

    var subtotal = cart.Sum(i => i.Price);
    Console.WriteLine($"  {"Item",-32} {"Price",9}  Notes");
    Console.WriteLine($"  {new string('-', 58)}");
    receipt.Print();
    Console.WriteLine($"  {new string('-', 58)}");
    Console.WriteLine($"  {"Subtotal",-32} ${subtotal,8:F2}");
    Console.WriteLine($"  {"Tax (HST)",-32} ${tax.TotalTax,8:F2}");
    Console.WriteLine($"  {"Shipping",-32} ${shipping.TotalShipping,8:F2}");
    Console.WriteLine($"  {new string('=', 44)}");
    Console.WriteLine($"  {"Order Total",-32} ${subtotal + tax.TotalTax + shipping.TotalShipping,8:F2}");
}
Pause();

// Demo 5 — New visitor added with ZERO changes to item classes
Console.WriteLine("── DEMO 5: New visitor — Loyalty Points (zero item changes) ──\n");
{
    // Imagine a new business requirement: award points per item type
    // Physical: 1 point per dollar | Digital: 2 points per dollar
    // Food: 0 points | Subscription: 50 flat points per month
    var points = new LoyaltyPointsVisitor();
    foreach (var item in cart)
        item.Accept(points);
    Console.WriteLine($"\n  Total points earned: {points.TotalPoints}");
}

Console.WriteLine("\n=== End of Visitor pattern demo ===");
