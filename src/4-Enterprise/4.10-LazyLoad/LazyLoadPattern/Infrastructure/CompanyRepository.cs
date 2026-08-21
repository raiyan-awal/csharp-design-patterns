using System.Data;
using Dapper;
using LazyLoadPattern.Domain;
using LazyLoadPattern.Proxies;

namespace LazyLoadPattern.Infrastructure;

public sealed class CompanyRepository
{
    private readonly IDbConnection _db;

    public CompanyRepository(IDbConnection db) => _db = db;

    // ── Lazy Initialization variant ───────────────────────────────────────────

    public Company Insert(string name, string industry, string city)
    {
        var id = _db.ExecuteScalar<int>(
            """
            INSERT INTO Companies (Name, Industry, City) VALUES (@Name, @Industry, @City);
            SELECT last_insert_rowid();
            """,
            new { Name = name, Industry = industry, City = city });
        return new Company(id, name, industry, city, () => LoadEmployees(id));
    }

    public Company? FindById(int id)
    {
        var row = _db.QuerySingleOrDefault<CompanyRow>(
            "SELECT * FROM Companies WHERE Id = @id", new { id });
        return row is null ? null : ToCompany(row);
    }

    public IReadOnlyList<Company> FindAll()
    {
        return _db.Query<CompanyRow>("SELECT * FROM Companies ORDER BY Name")
            .Select(ToCompany)
            .ToList();
    }

    // ── System.Lazy<T> variant ────────────────────────────────────────────────

    public LazyTCompany? FindByIdLazyT(int id)
    {
        var row = _db.QuerySingleOrDefault<CompanyRow>(
            "SELECT * FROM Companies WHERE Id = @id", new { id });
        return row is null ? null : ToLazyTCompany(row);
    }

    public IReadOnlyList<LazyTCompany> FindAllLazyT()
    {
        return _db.Query<CompanyRow>("SELECT * FROM Companies ORDER BY Name")
            .Select(ToLazyTCompany)
            .ToList();
    }

    // ── Virtual Proxy variant ─────────────────────────────────────────────────

    public CompanyProxy Proxy(int id) =>
        new(id, proxyId => FindById(proxyId)
            ?? throw new KeyNotFoundException($"Company {proxyId} not found."));

    // ── Employees ─────────────────────────────────────────────────────────────

    public Employee InsertEmployee(string name, string role, decimal salary, int companyId)
    {
        var id = _db.ExecuteScalar<int>(
            """
            INSERT INTO Employees (CompanyId, Name, Role, Salary)
            VALUES (@CompanyId, @Name, @Role, @Salary);
            SELECT last_insert_rowid();
            """,
            new { CompanyId = companyId, Name = name, Role = role, Salary = salary.ToString("F2") });
        return new Employee(id, companyId, name, role, salary);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Company ToCompany(CompanyRow row) =>
        new(row.Id, row.Name, row.Industry, row.City, () => LoadEmployees(row.Id));

    private LazyTCompany ToLazyTCompany(CompanyRow row) =>
        new(row.Id, row.Name, row.Industry, row.City, () => LoadEmployees(row.Id));

    private IReadOnlyList<Employee> LoadEmployees(int companyId)
    {
        return _db.Query<EmployeeRow>(
                "SELECT * FROM Employees WHERE CompanyId = @companyId ORDER BY Name",
                new { companyId })
            .Select(r => new Employee(r.Id, r.CompanyId, r.Name, r.Role, decimal.Parse(r.Salary)))
            .ToList();
    }

    private sealed class CompanyRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Industry { get; init; } = "";
        public string City { get; init; } = "";
    }

    private sealed class EmployeeRow
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public string Name { get; init; } = "";
        public string Role { get; init; } = "";
        public string Salary { get; init; } = "";
    }
}
