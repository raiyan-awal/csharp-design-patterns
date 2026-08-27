namespace EventSourcingPattern.Infrastructure;

public interface ISnapshotStore
{
    MemberSnapshot? Load(int memberId);
    void            Save(MemberSnapshot snapshot);
}
