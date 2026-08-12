using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public interface IMemberRepository
{
    Member? GetById(int id);
    Member? GetByMemberNumber(string memberNumber);
    IReadOnlyList<Member> GetAll();
    void Add(Member member);
    void Update(Member member);
}
