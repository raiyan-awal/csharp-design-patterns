using LazyLoadPattern.Domain;

namespace LazyLoadPattern.Proxies;

// Variant 3 — Virtual Proxy: holds only the Id at creation time.
// The real Company is loaded on the first access to any property beyond Id.
public sealed class CompanyProxy : ICompany
{
    private readonly Func<int, ICompany> _loader;
    private ICompany? _real;

    public int Id { get; }

    private ICompany Real => _real ??= _loader(Id);

    public string Name => Real.Name;
    public string Industry => Real.Industry;
    public string City => Real.City;
    public bool EmployeesLoaded => _real is not null && _real.EmployeesLoaded;
    public IReadOnlyList<Employee> Employees => Real.Employees;

    public bool IsLoaded => _real is not null;

    public CompanyProxy(int id, Func<int, ICompany> loader)
    {
        Id = id;
        _loader = loader;
    }
}
