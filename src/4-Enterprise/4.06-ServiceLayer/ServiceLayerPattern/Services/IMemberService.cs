using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Services;

public interface IMemberService
{
    Member RegisterMember(string name, string email);
    Member GetMember(int id);
    IReadOnlyList<Member> GetAllMembers();
    void DeactivateMember(int id);
    void ReactivateMember(int id);
}
