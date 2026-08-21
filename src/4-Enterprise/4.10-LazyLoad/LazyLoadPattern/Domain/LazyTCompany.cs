namespace LazyLoadPattern.Domain;

// Variant 2 — Value Holder via System.Lazy<T>: the idiomatic .NET approach.
// Thread-safe by default (LazyThreadSafetyMode.ExecutionAndPublication).
public sealed class LazyTCompany : ICompany
{
    private readonly Lazy<IReadOnlyList<Employee>> _employees;

    public int Id { get; }
    public string Name { get; }
    public string Industry { get; }
    public string City { get; }

    public bool EmployeesLoaded => _employees.IsValueCreated;

    public IReadOnlyList<Employee> Employees => _employees.Value;

    public LazyTCompany(int id, string name, string industry, string city,
                        Func<IReadOnlyList<Employee>> loadEmployees)
    {
        Id = id;
        Name = name;
        Industry = industry;
        City = city;
        _employees = new Lazy<IReadOnlyList<Employee>>(loadEmployees);
    }
}
