namespace EventSourcingPattern.Infrastructure;

public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Dictionary<int, MemberSnapshot> _snapshots = new();

    public MemberSnapshot? Load(int memberId) =>
        _snapshots.TryGetValue(memberId, out var s) ? s : null;

    public void Save(MemberSnapshot snapshot) => _snapshots[snapshot.MemberId] = snapshot;
}
