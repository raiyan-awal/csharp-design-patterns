using System.Data;
using Dapper;

namespace LazyLoadPattern.Infrastructure;

public static class Schema
{
    public static void Create(IDbConnection db)
    {
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Companies (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT    NOT NULL,
                Industry TEXT    NOT NULL,
                City     TEXT    NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Employees (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                CompanyId INTEGER NOT NULL,
                Name      TEXT    NOT NULL,
                Role      TEXT    NOT NULL,
                Salary    TEXT    NOT NULL
            );
            """);
    }
}
