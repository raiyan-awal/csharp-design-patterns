using System.Data;
using Dapper;

namespace IdentityMapPattern.Infrastructure;

public static class Schema
{
    public static void Create(IDbConnection db)
    {
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Artists (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL,
                Nationality TEXT    NOT NULL,
                BirthYear   INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Artworks (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Title        TEXT    NOT NULL,
                ArtistId     INTEGER NOT NULL,
                Medium       TEXT    NOT NULL,
                Year         INTEGER NOT NULL,
                ValuationCad TEXT    NOT NULL,
                OnDisplay    INTEGER NOT NULL DEFAULT 0
            );
            """);
    }
}
