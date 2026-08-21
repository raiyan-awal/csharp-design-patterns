namespace LazyLoadPattern.Domain;

public sealed class Employee
{
    public int Id { get; init; }
    public int CompanyId { get; init; }
    public string Name { get; init; }
    public string Role { get; init; }
    public decimal Salary { get; init; }

    public Employee(int id, int companyId, string name, string role, decimal salary)
    {
        Id = id;
        CompanyId = companyId;
        Name = name;
        Role = role;
        Salary = salary;
    }
}
