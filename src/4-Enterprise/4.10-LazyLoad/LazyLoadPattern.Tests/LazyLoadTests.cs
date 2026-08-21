using Microsoft.Data.Sqlite;
using LazyLoadPattern.Domain;
using LazyLoadPattern.Infrastructure;
using LazyLoadPattern.Proxies;

namespace LazyLoadPattern.Tests;

public sealed class LazyLoadTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CompanyRepository _repo;

    public LazyLoadTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Schema.Create(_connection);
        _repo = new CompanyRepository(_connection);
    }

    public void Dispose() => _connection.Dispose();

    // ── Company (Lazy Initialization) unit tests ──────────────────────────────

    private static Company MakeCompany(Func<IReadOnlyList<Employee>> loader) =>
        new(1, "Shopify", "E-Commerce", "Ottawa", loader);

    [Fact]
    public void Company_EmployeesLoaded_FalseBeforeFirstAccess()
    {
        var company = MakeCompany(() => []);

        Assert.False(company.EmployeesLoaded);
    }

    [Fact]
    public void Company_EmployeesLoaded_TrueAfterAccess()
    {
        var company = MakeCompany(() => []);

        _ = company.Employees;

        Assert.True(company.EmployeesLoaded);
    }

    [Fact]
    public void Company_LoaderCalledOnce_OnMultipleAccesses()
    {
        int callCount = 0;
        var company = MakeCompany(() => { callCount++; return []; });

        _ = company.Employees;
        _ = company.Employees;
        _ = company.Employees;

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Company_ReturnsEmployeesFromLoader()
    {
        var expected = new List<Employee>
        {
            new(1, 1, "Alice", "Engineer", 100_000m),
            new(2, 1, "Bob",   "Designer", 90_000m)
        };
        var company = MakeCompany(() => expected);

        var result = company.Employees;

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void Company_SecondAccess_ReturnsSameListInstance()
    {
        var company = MakeCompany(() => []);

        var first  = company.Employees;
        var second = company.Employees;

        Assert.Same(first, second);
    }

    // ── LazyTCompany (System.Lazy<T>) unit tests ──────────────────────────────

    private static LazyTCompany MakeLazyT(Func<IReadOnlyList<Employee>> loader) =>
        new(1, "RBC", "Finance", "Toronto", loader);

    [Fact]
    public void LazyT_IsValueCreated_FalseBeforeAccess()
    {
        var company = MakeLazyT(() => []);

        Assert.False(company.EmployeesLoaded);
    }

    [Fact]
    public void LazyT_IsValueCreated_TrueAfterAccess()
    {
        var company = MakeLazyT(() => []);

        _ = company.Employees;

        Assert.True(company.EmployeesLoaded);
    }

    [Fact]
    public void LazyT_LoaderCalledOnce()
    {
        int callCount = 0;
        var company = MakeLazyT(() => { callCount++; return []; });

        _ = company.Employees;
        _ = company.Employees;

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void LazyT_ReturnsCorrectEmployees()
    {
        var expected = new List<Employee> { new(1, 1, "Noah", "Analyst", 120_000m) };
        var company  = MakeLazyT(() => expected);

        Assert.Single(company.Employees);
        Assert.Equal("Noah", company.Employees[0].Name);
    }

    // ── CompanyProxy (Virtual Proxy) unit tests ───────────────────────────────

    [Fact]
    public void Proxy_DoesNotInvokeLoader_OnCreation()
    {
        int loaderCalls = 0;
        var proxy = new CompanyProxy(99, id =>
        {
            loaderCalls++;
            return MakeCompany(() => []);
        });

        Assert.Equal(0, loaderCalls);
        Assert.False(proxy.IsLoaded);
    }

    [Fact]
    public void Proxy_InvokesLoader_OnPropertyAccess()
    {
        int loaderCalls = 0;
        var proxy = new CompanyProxy(1, id =>
        {
            loaderCalls++;
            return MakeCompany(() => []);
        });

        _ = proxy.Name;

        Assert.Equal(1, loaderCalls);
        Assert.True(proxy.IsLoaded);
    }

    [Fact]
    public void Proxy_LoaderCalledOnce_OnMultiplePropertyAccesses()
    {
        int loaderCalls = 0;
        var proxy = new CompanyProxy(1, id =>
        {
            loaderCalls++;
            return MakeCompany(() => []);
        });

        _ = proxy.Name;
        _ = proxy.Industry;
        _ = proxy.Employees;

        Assert.Equal(1, loaderCalls);
    }

    // ── Integration tests (SQLite via CompanyRepository) ──────────────────────

    private Company SeedCompany(string name = "Suncor Energy") =>
        _repo.Insert(name, "Energy", "Calgary");

    [Fact]
    public void Repository_FindAll_DoesNotLoadEmployees()
    {
        SeedCompany("Shopify");
        SeedCompany("RBC Royal Bank");

        var companies = _repo.FindAll();

        Assert.All(companies, c => Assert.False(c.EmployeesLoaded));
    }

    [Fact]
    public void Repository_FindById_EmployeesNotLoaded_BeforeAccess()
    {
        var company = SeedCompany();

        var found = _repo.FindById(company.Id)!;

        Assert.False(found.EmployeesLoaded);
    }

    [Fact]
    public void Repository_FindById_EmployeesLoadedOnAccess()
    {
        var company = SeedCompany();
        _repo.InsertEmployee("James Liu", "Engineer", 140_000m, company.Id);
        _repo.InsertEmployee("Chloe Patel", "Analyst", 125_000m, company.Id);

        var found = _repo.FindById(company.Id)!;
        var employees = found.Employees;

        Assert.True(found.EmployeesLoaded);
        Assert.Equal(2, employees.Count);
    }

    [Fact]
    public void Repository_TwoCompanies_LoadEmployees_Independently()
    {
        var a = SeedCompany("Company A");
        var b = SeedCompany("Company B");
        _repo.InsertEmployee("Alice", "Dev", 100_000m, a.Id);
        _repo.InsertEmployee("Bob",   "Dev", 100_000m, b.Id);

        var companies = _repo.FindAll();
        var compA = companies.First(c => c.Name == "Company A");

        _ = compA.Employees;  // only Company A's employees load

        Assert.True(compA.EmployeesLoaded);
        Assert.False(companies.First(c => c.Name == "Company B").EmployeesLoaded);
    }

    [Fact]
    public void Repository_FindByIdLazyT_BehavesLikeLazyInit()
    {
        var company = SeedCompany();
        _repo.InsertEmployee("Ethan", "Officer", 110_000m, company.Id);

        var lazyT = _repo.FindByIdLazyT(company.Id)!;

        Assert.False(lazyT.EmployeesLoaded);
        Assert.Single(lazyT.Employees);
        Assert.True(lazyT.EmployeesLoaded);
    }

    [Fact]
    public void InsertEmployee_ReturnsWithId()
    {
        var company  = SeedCompany();
        var employee = _repo.InsertEmployee("Ava Singh", "Aerospace Engineer", 130_000m, company.Id);

        Assert.True(employee.Id > 0);
        Assert.Equal(company.Id, employee.CompanyId);
        Assert.Equal(130_000m, employee.Salary);
    }

    [Fact]
    public void Employee_AllFields_RoundTrip()
    {
        var company  = SeedCompany();
        _repo.InsertEmployee("Mason Roy", "Systems Integrator", 118_000m, company.Id);

        var employees = _repo.FindById(company.Id)!.Employees;

        Assert.Single(employees);
        var e = employees[0];
        Assert.Equal("Mason Roy", e.Name);
        Assert.Equal("Systems Integrator", e.Role);
        Assert.Equal(118_000m, e.Salary);
        Assert.Equal(company.Id, e.CompanyId);
    }
}
