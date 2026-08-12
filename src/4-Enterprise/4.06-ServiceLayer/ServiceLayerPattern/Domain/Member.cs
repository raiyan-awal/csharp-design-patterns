namespace ServiceLayerPattern.Domain;

public sealed class Member
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string MemberNumber { get; init; } = "";
    public DateTime JoinedAt { get; init; }
    public bool IsActive { get; private set; } = true;

    public Member(int id, string name, string email, string memberNumber, DateTime joinedAt)
    {
        Id = id;
        Name = name;
        Email = email;
        MemberNumber = memberNumber;
        JoinedAt = joinedAt;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
