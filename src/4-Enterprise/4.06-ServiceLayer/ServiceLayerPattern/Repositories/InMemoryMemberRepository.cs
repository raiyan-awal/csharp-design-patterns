using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public sealed class InMemoryMemberRepository : IMemberRepository
{
    private readonly List<Member> _members = [];

    public Member? GetById(int id) => _members.FirstOrDefault(m => m.Id == id);

    public Member? GetByMemberNumber(string memberNumber) =>
        _members.FirstOrDefault(m => m.MemberNumber == memberNumber);

    public IReadOnlyList<Member> GetAll() => _members.AsReadOnly();

    public void Add(Member member) => _members.Add(member);

    public void Update(Member member)
    {
        var index = _members.FindIndex(m => m.Id == member.Id);
        if (index >= 0) _members[index] = member;
    }
}
