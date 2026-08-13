using System.Data;

namespace ActiveRecordPattern.Infrastructure;

public static class Database
{
    private static IDbConnection? _connection;

    public static IDbConnection Connection => _connection
        ?? throw new InvalidOperationException("Call Database.Initialize before accessing the database.");

    public static void Initialize(IDbConnection connection) => _connection = connection;
}
