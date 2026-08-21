namespace LazyLoadPattern.Domain;

public interface ICompany
{
    int Id { get; }
    string Name { get; }
    string Industry { get; }
    string City { get; }
    bool EmployeesLoaded { get; }
    IReadOnlyList<Employee> Employees { get; }
}
