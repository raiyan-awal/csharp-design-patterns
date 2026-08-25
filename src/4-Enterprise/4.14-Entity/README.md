# 4.14 — Entity

## Intent

An Entity is a domain object defined by its unique identity rather than by its attributes. Two entities are equal if and only if they share the same ID — even if every other attribute differs. That identity persists through time: the same entity can change its name, address, or status and remain the same object in the domain model.

## The Problem It Solves

```csharp
// Without an Entity base class: equality falls back to reference equality
var p1 = new Patient { Name = "Sophie Tremblay", HealthCard = "TREM-001" };
var p2 = new Patient { Name = "Sophie Tremblay", HealthCard = "TREM-001" };

bool same = p1 == p2;   // false — different object references, even though same person
p1.Name = "Sophie Bergeron-Tremblay";  // Is this still Sophie? No way to tell.
```

Problems:

- **Reference equality is meaningless for domain objects.** Two separate `Patient` objects loaded from a database for the same person should be equal — but default C# object equality says they are not.
- **No way to track identity through state changes.** If a patient changes their name, reference-based code treats the updated object as a different entity, breaking caches, sets, and equality checks.
- **Type confusion is undetected.** Without a typed equality contract, a `Patient` with ID 1 and a `Doctor` with ID 1 compare as equal if only IDs are checked, corrupting collections that hold mixed types.
- **Repeated boilerplate.** Every domain class that needs identity-based equality must re-implement `Equals`, `GetHashCode`, and the `==`/`!=` operators independently.

## Solution: Generic Entity Base Class

```csharp
// Entity<TId> centralises identity-based equality once — all domain classes inherit it
var p1 = new Patient(1, "TREM-001", "Sophie Tremblay", ...);
var p2 = new Patient(1, "TREM-001", "Sophie Tremblay", ...);  // different object, same ID

bool same = p1 == p2;   // true — same identity
p1.UpdateName("Sophie Bergeron-Tremblay");
bool stillSame = p1 == p2;  // true — identity unchanged by state change
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Entity Base | `Entity<TId>` | Provides `Id`, identity-based `Equals`/`GetHashCode`/`==`/`!=`; enforces type safety |
| Entity | `Patient` | Domain object with health card number, name, DOB, address; state can change |
| Entity | `Doctor` | Domain object with medical licence number, name, specialization |
| Entity | `Appointment` | Links a `Patient` and `Doctor` by ID (not by object reference); tracks status lifecycle |
| Value | `AppointmentStatus` | Enum: `Scheduled`, `Completed`, `Cancelled` |
| Repository | `IPatientRepository` | Finds patients by ID or health card number |
| Repository | `IDoctorRepository` | Finds doctors by ID or specialization |
| Repository | `IAppointmentRepository` | Finds appointments by patient ID or doctor ID |

## Structure

```
4.14-Entity/
├── EntityPattern/
│   ├── Domain/
│   │   ├── Entity.cs              ← generic base: Id, Equals, GetHashCode, ==, !=
│   │   ├── Patient.cs             ← entity: health card identity, mutable name/address
│   │   ├── Doctor.cs              ← entity: licence number identity, mutable specialization
│   │   ├── Appointment.cs         ← entity: references Patient and Doctor by ID only
│   │   └── AppointmentStatus.cs   ← Scheduled / Completed / Cancelled
│   ├── Repositories/
│   │   ├── IPatientRepository.cs
│   │   ├── IDoctorRepository.cs
│   │   ├── IAppointmentRepository.cs
│   │   ├── InMemoryPatientRepository.cs
│   │   ├── InMemoryDoctorRepository.cs
│   │   └── InMemoryAppointmentRepository.cs
│   └── Program.cs                 ← 4-section demo
└── EntityPattern.Tests/
    └── EntityTests.cs             ← 37 tests across 5 suites
```

## Key Code

### Entity\<TId\> — identity-based equality in one place

```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);
}
```

Three guards in `Equals`: reference shortcut (same object is always equal), type guard (`Patient(1) != Doctor(1)` even with the same ID), then ID comparison. `HashCode.Combine(GetType(), Id)` mirrors the type guard — `Patient(1)` and `Doctor(1)` produce different hash codes and can safely coexist in the same `HashSet` or dictionary.

### Patient — state changes that do not affect identity

```csharp
public sealed class Patient : Entity<int>
{
    public string FullName { get; private set; }

    public void UpdateName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        FullName = fullName;
    }
}
```

`FullName` is mutable; `Id` and `HealthCardNumber` are not. A patient who marries and changes their surname is still the same `Patient` — the `Id` never changes, so every equality check, cache lookup, and set membership test continues to work correctly.

### Appointment — reference by ID, not by object

```csharp
public sealed class Appointment : Entity<int>
{
    public int PatientId { get; }
    public int DoctorId  { get; }
    // ...
}
```

`Appointment` stores `PatientId` and `DoctorId` as plain integers, not as `Patient` and `Doctor` object references. This is the standard DDD guidance: entities reference each other by identity, not by pointer. It prevents tight object-graph coupling, lets each entity be loaded independently, and maps naturally to how foreign keys work in a relational database. To resolve the full objects, callers look up `PatientId` in `IPatientRepository`.

### Appointment lifecycle — status transitions with guards

```csharp
public void Complete(string notes)
{
    if (Status != AppointmentStatus.Scheduled)
        throw new InvalidOperationException(
            $"Cannot complete an appointment that is already {Status}.");
    Status = AppointmentStatus.Completed;
    Notes  = notes;
}
```

`Complete` and `Cancel` each guard against invalid state transitions. An appointment can only be completed or cancelled while it is `Scheduled`; trying either operation on an already-terminal status throws.

## Demo Scenarios

```
=== Maple Street Medical Centre — Entity Pattern Demo ===

--- Identity vs Attributes ---
  Patient #1: Sophie Tremblay
  Patient #2: Sophie Tremblay
  p1 == p2 (same name, different ID):   False
  p1 == p3 (different object, same ID): True
  Patient(1).GetHashCode() == Patient(1).GetHashCode(): True
  Patient(Id=1) == Doctor(Id=1) (same ID, different type): False

--- State Changes Preserve Identity ---
  Before update — Id: 1, Name: Sophie Tremblay
  After  update — Id: 1, Name: Sophie Bergeron-Tremblay
  Identity unchanged: p1.Id is still 1
  p1 == p3 (p3 still has old name, same Id): True

--- Reference by ID ---
  Appointments reference Patient and Doctor by ID, not by object.
  Apt #1 | Sophie Bergeron-Tremblay → Dr. Amara Okonkwo (Family Medicine)
           Reason: Annual physical — blood pressure follow-up
  Apt #2 | Sophie Tremblay → Dr. James Osei (Family Medicine)
           Reason: Persistent cough — possible respiratory infection

--- Repository Round-Trip and Appointment Lifecycle ---
  Apt #1 status after complete: Completed
  Notes: BP: 122/78. Weight stable. Booked follow-up in 6 months.
  Apt #2 status after cancel: Cancelled
  Dr. Okonkwo's appointments: #1 — Completed
  All Family Medicine doctors: #1 — Dr. Amara Okonkwo, #3 — Dr. James Osei
```

## When to Use

- A domain object needs to be tracked across time and through state changes — the "same thing" even after its attributes are updated.
- Two separate object instances loaded from the database for the same record must compare as equal.
- You need multiple domain classes with consistent identity semantics without duplicating `Equals`/`GetHashCode` boilerplate.
- You are modelling DDD entities and need a clear distinction between objects-with-identity and value objects (which are equal when their attributes match).

## When NOT to Use

- The object has no meaningful identity — it is defined entirely by its values (a `Money` amount, an `Address`). Use a **Value Object** instead.
- The object is a pure data-transfer container with no domain behaviour. A DTO does not need identity-based equality.
- You need only one class with identity and it will never be confused with another type. The generic base is most valuable when several domain classes share the same equality contract.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Single equality implementation | `Equals`/`GetHashCode`/`==`/`!=` are written once in the base class and inherited by every entity. |
| Type-safe equality | The `GetType()` guard prevents `Patient(1) == Doctor(1)` — they share an ID but are different domain concepts. |
| Identity survives state changes | Mutable attributes change freely; the ID never changes, so caches and collections stay correct. |
| Natural collection membership | Entities with the same ID hash to the same bucket and compare as equal in `HashSet<T>` and `Dictionary<TKey, TValue>`. |
| Loose coupling via ID references | Entities hold only the IDs of their collaborators, mirroring foreign keys and enabling independent loading. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| ID assignment responsibility | Someone must assign unique IDs before construction — a database sequence, a GUID generator, or an in-memory counter. The base class does not provide this. |
| No new-entity sentinel | There is no built-in "transient" state (ID not yet assigned). Some implementations use `Id == 0` or `Id == default` as a sentinel; this must be enforced by convention. |
| Generic type parameter friction | `Entity<TId>` requires every consumer to know the ID type. Mixed ID types (int vs Guid) in the same collection require a non-generic base or interface. |

## Related Patterns

- **Value Object (4.11)** — the direct contrast: a value object is equal when all its attributes match and has no identity. Use value objects for concepts like `Money` or `Address` that compose an entity's state.
- **Aggregate Root (4.13)** — an aggregate root is a special entity that owns a cluster of sub-objects and enforces consistency rules across them. Every aggregate root is an entity, but not every entity is an aggregate root.
- **Repository (4.01)** — repositories retrieve entities by their ID; the entity's `Equals` override ensures that a freshly loaded copy compares equal to one already in memory.
- **Identity Map (4.09)** — uses entity identity to guarantee that only one in-memory object exists per ID; relies on the same ID-based equality that the Entity base class provides.
- **Domain Event (4.12)** — domain events carry the entity's ID (not an object reference) as their payload, which is the same "reference by ID" principle applied to the event bus.

## Running the Demo

```bash
cd src/4-Enterprise/4.14-Entity/EntityPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.14-Entity/EntityPattern.Tests && dotnet test
```
