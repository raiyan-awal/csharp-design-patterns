using ServiceLayerPattern.Domain;
using ServiceLayerPattern.Repositories;

namespace ServiceLayerPattern.Services;

public sealed class MemberService(IMemberRepository members) : IMemberService
{
    private int _nextId = 1;
    private int _memberNumberSeed = 1000;

    public Member RegisterMember(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

        var memberNumber = $"TPL-{_memberNumberSeed++}";
        var member = new Member(_nextId++, name, email, memberNumber, DateTime.UtcNow);
        members.Add(member);
        return member;
    }

    public Member GetMember(int id) =>
        members.GetById(id) ?? throw new KeyNotFoundException($"Member {id} not found.");

    public IReadOnlyList<Member> GetAllMembers() => members.GetAll();

    public void DeactivateMember(int id)
    {
        var member = GetMember(id);
        member.Deactivate();
        members.Update(member);
    }

    public void ReactivateMember(int id)
    {
        var member = GetMember(id);
        member.Reactivate();
        members.Update(member);
    }
}
