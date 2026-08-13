using System.Data;
using Dapper;

namespace DataMapperPattern.Mappers;

public static class Schema
{
    public static void Create(IDbConnection db)
    {
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Films (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Title           TEXT    NOT NULL,
                Director        TEXT    NOT NULL,
                Genre           TEXT    NOT NULL,
                ReleaseYear     INTEGER NOT NULL,
                RuntimeMinutes  INTEGER NOT NULL,
                CertifiedFresh  INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Reviews (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                FilmId          INTEGER NOT NULL REFERENCES Films(Id),
                ReviewerName    TEXT    NOT NULL,
                Score           INTEGER NOT NULL CHECK(Score BETWEEN 1 AND 10),
                Comment         TEXT    NOT NULL,
                ReviewedAt      TEXT    NOT NULL
            );
            """);
    }
}
