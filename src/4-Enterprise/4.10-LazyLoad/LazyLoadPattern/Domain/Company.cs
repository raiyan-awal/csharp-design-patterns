namespace LazyLoadPattern.Domain;

// Variant 1 — Lazy Initialization: field starts null; loaded on first property access.
public sealed class Company : ICompany
{
    private readonly Func<IReadOnlyList<Employee>> _loadEmployees;
    private IReadOnlyList<Employee>? _employees;

    public int Id { get; }
    public string Name { get; }
    public string Industry { get; }
    public string City { get; }

    public bool EmployeesLoaded => _employees is not null;

    public IReadOnlyList<Employee> Employees
    {
        get
        {
            _employees ??= _loadEmployees();
            return _employees;
        }
    }

    public Company(int id, string name, string industry, string city,
                   Func<IReadOnlyList<Employee>> loadEmployees)
    {
        Id = id;
        Name = name;
        Industry = industry;
        City = city;
        _loadEmployees = loadEmployees;
    }
}
