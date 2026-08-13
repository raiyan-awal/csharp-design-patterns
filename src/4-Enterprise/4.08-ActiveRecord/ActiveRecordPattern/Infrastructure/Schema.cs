using Dapper;

namespace ActiveRecordPattern.Infrastructure;

public static class Schema
{
    public static void Create()
    {
        Database.Connection.Execute("""
            CREATE TABLE IF NOT EXISTS RentalUnits (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Address     TEXT    NOT NULL,
                City        TEXT    NOT NULL,
                Province    TEXT    NOT NULL,
                MonthlyRent TEXT    NOT NULL,
                Bedrooms    INTEGER NOT NULL,
                IsAvailable INTEGER NOT NULL DEFAULT 1,
                LastUpdated TEXT    NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Tenants (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Name         TEXT    NOT NULL,
                Email        TEXT    NOT NULL,
                Phone        TEXT    NOT NULL,
                RentalUnitId INTEGER NOT NULL,
                LeaseStart   TEXT    NOT NULL,
                LeaseEnd     TEXT    NOT NULL
            );
            """);
    }
}
