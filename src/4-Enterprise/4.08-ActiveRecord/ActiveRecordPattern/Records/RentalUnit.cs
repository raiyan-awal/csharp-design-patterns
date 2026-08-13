using Dapper;
using ActiveRecordPattern.Infrastructure;

namespace ActiveRecordPattern.Records;

public sealed class RentalUnit
{
    public int Id { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string Province { get; private set; }
    public decimal MonthlyRent { get; private set; }
    public int Bedrooms { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTime LastUpdated { get; private set; }

    public RentalUnit(string address, string city, string province, decimal monthlyRent, int bedrooms)
    {
        Address = address;
        City = city;
        Province = province;
        MonthlyRent = monthlyRent;
        Bedrooms = bedrooms;
        IsAvailable = true;
        LastUpdated = DateTime.UtcNow;
    }

    private RentalUnit(int id, string address, string city, string province,
                       decimal monthlyRent, int bedrooms, bool isAvailable, DateTime lastUpdated)
    {
        Id = id;
        Address = address;
        City = city;
        Province = province;
        MonthlyRent = monthlyRent;
        Bedrooms = bedrooms;
        IsAvailable = isAvailable;
        LastUpdated = lastUpdated;
    }

    public void Rent()
    {
        if (!IsAvailable)
            throw new InvalidOperationException($"'{Address}' is already rented.");
        IsAvailable = false;
        LastUpdated = DateTime.UtcNow;
        Save();
    }

    public void Vacate()
    {
        IsAvailable = true;
        LastUpdated = DateTime.UtcNow;
        Save();
    }

    public void UpdateRent(decimal newMonthlyRent)
    {
        if (newMonthlyRent <= 0)
            throw new ArgumentException("Monthly rent must be positive.", nameof(newMonthlyRent));
        MonthlyRent = newMonthlyRent;
        LastUpdated = DateTime.UtcNow;
        Save();
    }

    public void Save()
    {
        if (Id == 0)
        {
            Id = Database.Connection.ExecuteScalar<int>(
                """
                INSERT INTO RentalUnits (Address, City, Province, MonthlyRent, Bedrooms, IsAvailable, LastUpdated)
                VALUES (@Address, @City, @Province, @MonthlyRent, @Bedrooms, @IsAvailable, @LastUpdated);
                SELECT last_insert_rowid();
                """,
                new
                {
                    Address,
                    City,
                    Province,
                    MonthlyRent = MonthlyRent.ToString("F2"),
                    Bedrooms,
                    IsAvailable = IsAvailable ? 1 : 0,
                    LastUpdated = LastUpdated.ToString("O")
                });
        }
        else
        {
            Database.Connection.Execute(
                """
                UPDATE RentalUnits
                SET Address = @Address, City = @City, Province = @Province,
                    MonthlyRent = @MonthlyRent, Bedrooms = @Bedrooms,
                    IsAvailable = @IsAvailable, LastUpdated = @LastUpdated
                WHERE Id = @Id
                """,
                new
                {
                    Address,
                    City,
                    Province,
                    MonthlyRent = MonthlyRent.ToString("F2"),
                    Bedrooms,
                    IsAvailable = IsAvailable ? 1 : 0,
                    LastUpdated = LastUpdated.ToString("O"),
                    Id
                });
        }
    }

    public void Delete()
    {
        Database.Connection.Execute("DELETE FROM RentalUnits WHERE Id = @Id", new { Id });
    }

    public static RentalUnit? FindById(int id)
    {
        var row = Database.Connection.QuerySingleOrDefault<Row>(
            "SELECT * FROM RentalUnits WHERE Id = @id", new { id });
        return row?.ToUnit();
    }

    public static IReadOnlyList<RentalUnit> FindAll()
    {
        return Database.Connection
            .Query<Row>("SELECT * FROM RentalUnits ORDER BY City, Address")
            .Select(r => r.ToUnit())
            .ToList();
    }

    public static IReadOnlyList<RentalUnit> FindAvailable()
    {
        return Database.Connection
            .Query<Row>("SELECT * FROM RentalUnits WHERE IsAvailable = 1 ORDER BY MonthlyRent")
            .Select(r => r.ToUnit())
            .ToList();
    }

    public static IReadOnlyList<RentalUnit> FindByCity(string city)
    {
        return Database.Connection
            .Query<Row>("SELECT * FROM RentalUnits WHERE City = @city ORDER BY MonthlyRent", new { city })
            .Select(r => r.ToUnit())
            .ToList();
    }

    private sealed class Row
    {
        public int Id { get; init; }
        public string Address { get; init; } = "";
        public string City { get; init; } = "";
        public string Province { get; init; } = "";
        public string MonthlyRent { get; init; } = "";
        public int Bedrooms { get; init; }
        public int IsAvailable { get; init; }
        public string LastUpdated { get; init; } = "";

        public RentalUnit ToUnit() => new(
            Id,
            Address,
            City,
            Province,
            decimal.Parse(MonthlyRent),
            Bedrooms,
            IsAvailable != 0,
            DateTime.Parse(LastUpdated, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }
}
