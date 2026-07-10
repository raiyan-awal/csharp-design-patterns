using StatePattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static VendingMachine BuildMachine()
{
    var m = new VendingMachine();
    m.AddProduct("A1", "Water",    1.00m, 3);
    m.AddProduct("A2", "Cola",     1.50m, 2);
    m.AddProduct("B1", "Chips",    1.20m, 2);
    m.AddProduct("B2", "Chocolate",2.00m, 1);
    return m;
}

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== State Pattern — Vending Machine ===\n");

// Demo 1 — Happy path: exact change
Console.WriteLine("── DEMO 1: Happy path — exact change ──");
{
    var machine = BuildMachine();
    machine.InsertMoney(1.50m);
    machine.SelectProduct("A2");   // Cola $1.50 — exact change
    Console.WriteLine($"  State after: {machine.StateName}");
}
Pause();

// Demo 2 — Happy path: overpay → change returned
Console.WriteLine("── DEMO 2: Overpay — change returned ──");
{
    var machine = BuildMachine();
    machine.InsertMoney(1.00m);
    machine.InsertMoney(0.50m);    // top-up while in HasMoney state
    machine.InsertMoney(0.50m);    // another top-up; balance now $2.00
    machine.SelectProduct("B1");   // Chips $1.20 → change: $0.80
    Console.WriteLine($"  State after: {machine.StateName}");
}
Pause();

// Demo 3 — Insufficient funds → add more → purchase
Console.WriteLine("── DEMO 3: Insufficient funds — top up and retry ──");
{
    var machine = BuildMachine();
    machine.InsertMoney(0.50m);
    machine.SelectProduct("B2");   // Chocolate $2.00 — not enough
    machine.InsertMoney(1.50m);    // add more; balance now $2.00
    machine.SelectProduct("B2");   // succeeds
    Console.WriteLine($"  State after: {machine.StateName}");
}
Pause();

// Demo 4 — Cancel purchase (return money)
Console.WriteLine("── DEMO 4: Cancel — money returned ──");
{
    var machine = BuildMachine();
    machine.InsertMoney(2.00m);
    machine.ReturnMoney();
    Console.WriteLine($"  Balance after return: ${machine.Balance:F2}");
    Console.WriteLine($"  State after: {machine.StateName}");
}
Pause();

// Demo 5 — Invalid operations in wrong states (guard behaviour)
Console.WriteLine("── DEMO 5: Invalid operations — state guards ──");
{
    var machine = BuildMachine();

    Console.WriteLine("  [Idle state guards]");
    machine.SelectProduct("A1");   // no money yet
    machine.ReturnMoney();         // nothing to return
    machine.Dispense();            // nothing selected

    machine.InsertMoney(1.00m);
    Console.WriteLine("  [HasMoney state guards]");
    machine.Dispense();            // no product selected yet
    machine.SelectProduct("X9");   // non-existent product
    machine.SelectProduct("B2");   // $2.00 — insufficient (have $1.00)

    machine.ReturnMoney();         // reset back to Idle
}
Pause();

// Demo 6 — Out-of-stock: deplete all stock then try to buy and restock
Console.WriteLine("── DEMO 6: Out of stock → restock ──");
{
    var machine = new VendingMachine();
    machine.AddProduct("A1", "Water", 1.00m, 1); // only 1 unit

    machine.InsertMoney(1.00m);
    machine.SelectProduct("A1");   // dispenses last item → OutOfStock

    Console.WriteLine($"  State after last item: {machine.StateName}");

    Console.WriteLine("  Trying to buy while out of stock:");
    machine.InsertMoney(1.00m);    // rejected

    Console.WriteLine("  Restocking...");
    machine.RestockProduct("A1", 3);
    Console.WriteLine($"  State after restock: {machine.StateName}");

    machine.InsertMoney(1.00m);
    machine.SelectProduct("A1");   // works again
    Console.WriteLine($"  State after purchase: {machine.StateName}");
}
Pause();

// Demo 7 — Out-of-stock with money in machine → money returned on restock path
Console.WriteLine("── DEMO 7: Money in machine when it goes out of stock ──");
{
    var machine = new VendingMachine();
    machine.AddProduct("A1", "Water", 1.00m, 1);

    machine.InsertMoney(5.00m);    // insert lots of money
    machine.SelectProduct("A1");   // dispenses last item → OutOfStock (with change)
    Console.WriteLine($"  State: {machine.StateName}  Balance: ${machine.Balance:F2}");
}

Console.WriteLine("\n=== End of State pattern demo ===");
