using Dapper;
using ActiveRecordPattern.Infrastructure;

namespace ActiveRecordPattern.Records;

public sealed class Tenant
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public int RentalUnitId { get; private set; }
    public DateTime LeaseStart { get; private set; }
    public DateTime LeaseEnd { get; private set; }

    public bool IsLeaseExpired => DateTime.UtcNow > LeaseEnd;
    public int DaysUntilExpiry => (LeaseEnd - DateTime.UtcNow).Days;

    public Tenant(string name, string email, string phone, int rentalUnitId,
                  DateTime leaseStart, DateTime leaseEnd)
    {
        if (leaseEnd <= leaseStart)
            throw new ArgumentException("Lease end must be after lease start.", nameof(leaseEnd));
        Name = name;
        Email = email;
        Phone = phone;
        RentalUnitId = rentalUnitId;
        LeaseStart = leaseStart;
        LeaseEnd = leaseEnd;
    }

    private Tenant(int id, string name, string email, string phone, int rentalUnitId,
                   DateTime leaseStart, DateTime leaseEnd)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        RentalUnitId = rentalUnitId;
        LeaseStart = leaseStart;
        LeaseEnd = leaseEnd;
    }

    public void ExtendLease(int months)
    {
        if (months <= 0)
            throw new ArgumentException("Extension must be at least 1 month.", nameof(months));
        LeaseEnd = LeaseEnd.AddMonths(months);
        Save();
    }

    public void Save()
    {
        if (Id == 0)
        {
            Id = Database.Connection.ExecuteScalar<int>(
                """
                INSERT INTO Tenants (Name, Email, Phone, RentalUnitId, LeaseStart, LeaseEnd)
                VALUES (@Name, @Email, @Phone, @RentalUnitId, @LeaseStart, @LeaseEnd);
                SELECT last_insert_rowid();
                """,
                new
                {
                    Name,
                    Email,
                    Phone,
                    RentalUnitId,
                    LeaseStart = LeaseStart.ToString("O"),
                    LeaseEnd = LeaseEnd.ToString("O")
                });
        }
        else
        {
            Database.Connection.Execute(
                """
                UPDATE Tenants
                SET Name = @Name, Email = @Email, Phone = @Phone,
                    RentalUnitId = @RentalUnitId, LeaseStart = @LeaseStart, LeaseEnd = @LeaseEnd
                WHERE Id = @Id
                """,
                new
                {
                    Name,
                    Email,
                    Phone,
                    RentalUnitId,
                    LeaseStart = LeaseStart.ToString("O"),
                    LeaseEnd = LeaseEnd.ToString("O"),
                    Id
                });
        }
    }

    public void Delete()
    {
        Database.Connection.Execute("DELETE FROM Tenants WHERE Id = @Id", new { Id });
    }

    public static Tenant? FindById(int id)
    {
        var row = Database.Connection.QuerySingleOrDefault<Row>(
            "SELECT * FROM Tenants WHERE Id = @id", new { id });
        return row?.ToTenant();
    }

    public static IReadOnlyList<Tenant> FindAll()
    {
        return Database.Connection
            .Query<Row>("SELECT * FROM Tenants ORDER BY Name")
            .Select(r => r.ToTenant())
            .ToList();
    }

    public static IReadOnlyList<Tenant> FindByUnit(int rentalUnitId)
    {
        return Database.Connection
            .Query<Row>("SELECT * FROM Tenants WHERE RentalUnitId = @rentalUnitId ORDER BY Name",
                        new { rentalUnitId })
            .Select(r => r.ToTenant())
            .ToList();
    }

    private sealed class Row
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Email { get; init; } = "";
        public string Phone { get; init; } = "";
        public int RentalUnitId { get; init; }
        public string LeaseStart { get; init; } = "";
        public string LeaseEnd { get; init; } = "";

        public Tenant ToTenant() => new(
            Id,
            Name,
            Email,
            Phone,
            RentalUnitId,
            DateTime.Parse(LeaseStart, null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(LeaseEnd, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }
}
