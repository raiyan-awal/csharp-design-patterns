# C# Design Patterns Reference

**Your one-stop repository for learning and revisiting design patterns in C# / .NET 10**

This repository contains **57 design patterns** with practical examples, detailed explanations, and comprehensive unit tests. Every pattern includes extensive code comments to help you understand not just *what* the code does, but *why* and *when* to use each pattern.

---

## 📚 Table of Contents

- [Pattern Categories](#pattern-categories)
- [Repository Structure](#repository-structure)
- [How to Use This Repository](#how-to-use-this-repository)
- [Progress Tracking](#progress-tracking)
- [Pattern Reference](#pattern-reference)
- [Language-Specific Patterns](LANGUAGE_SPECIFIC_PATTERNS.md) *(Java, Go, Rust, PHP, Node.js, Python, Rails, Elixir, Kotlin — reference only)*

---

## Pattern Categories

This repository covers four major categories of design patterns:

### 1️⃣ Creational Patterns (6)
Patterns that deal with object creation mechanisms, trying to create objects in a manner suitable to the situation.

### 2️⃣ Structural Patterns (7)
Patterns that deal with object composition, creating relationships between objects to form larger structures.

### 3️⃣ Behavioral Patterns (13)
Patterns that deal with communication between objects, how objects interact and distribute responsibility.

### 4️⃣ Enterprise / Architectural Patterns (31)
Patterns commonly used in enterprise applications, focusing on scalability, maintainability, and clean architecture.

---

## Repository Structure

Each pattern is **self-contained** — the implementation and its tests live together in the same folder:

```
csharp-design-patterns/
├── README.md
├── PROGRESS.md                          # Implementation status for all 57 patterns
├── LANGUAGE_SPECIFIC_PATTERNS.md        # Reference: Java, Go, Rust, PHP, Node.js, etc.
├── DesignPatterns.slnx                  # Solution file — run all tests from the root
└── src/
    ├── 1-Creational/
    │   ├── 1.1-Singleton/
    │   │   ├── SingletonPattern/        ← implementation (dotnet run)
    │   │   ├── SingletonPattern.Tests/  ← tests       (dotnet test)
    │   │   └── README.md
    │   ├── 1.2-FactoryMethod/
    │   │   ├── FactoryMethodPattern/
    │   │   ├── FactoryMethodPattern.Tests/
    │   │   └── README.md
    │   ├── 1.3-AbstractFactory/
    │   │   ├── AbstractFactoryPattern/
    │   │   ├── AbstractFactoryPattern.Tests/
    │   │   └── README.md
    │   ├── 1.4-Builder/
    │   │   ├── BuilderPattern/
    │   │   ├── BuilderPattern.Tests/
    │   │   └── README.md
    │   ├── 1.5-Prototype/
    │   │   ├── PrototypePattern/
    │   │   ├── PrototypePattern.Tests/
    │   │   └── README.md
    │   └── 1.6-ObjectPool/
    │       ├── ObjectPoolPattern/
    │       ├── ObjectPoolPattern.Tests/
    │       └── README.md
    ├── 2-Structural/
    │   ├── 2.1-Adapter/
    │   ├── 2.2-Bridge/
    │   ├── 2.3-Composite/
    │   ├── 2.4-Decorator/
    │   ├── 2.5-Facade/
    │   ├── 2.6-Flyweight/
    │   └── 2.7-Proxy/
    ├── 3-Behavioral/
    │   ├── 3.01-ChainOfResponsibility/
    │   ├── 3.02-Command/
    │   ├── 3.03-Interpreter/
    │   ├── 3.04-Iterator/
    │   ├── 3.05-Mediator/
    │   ├── 3.06-Memento/
    │   ├── 3.07-Observer/
    │   ├── 3.08-State/
    │   ├── 3.09-Strategy/
    │   ├── 3.10-TemplateMethod/
    │   ├── 3.11-Visitor/
    │   ├── 3.12-NullObject/
    │   └── 3.13-Pipeline/
    └── 4-Enterprise/
        ├── 4.01-Repository/
        ├── 4.02-UnitOfWork/
        ├── 4.03-CQRS/
        ├── 4.04-Specification/
        ├── 4.05-DependencyInjection/
        ├── 4.06-ServiceLayer/
        ├── 4.07-DataMapper/
        ├── 4.08-ActiveRecord/
        ├── 4.09-IdentityMap/
        ├── 4.10-LazyLoad/
        ├── 4.11-ValueObject/
        ├── 4.12-DomainEvent/
        ├── 4.13-AggregateRoot/
        ├── 4.14-Entity/
        ├── 4.15-EventSourcing/
        ├── 4.16-CircuitBreaker/
        ├── 4.17-RetryPattern/
        ├── 4.18-Bulkhead/
        ├── 4.19-SagaPattern/
        ├── 4.20-OutboxPattern/
        ├── 4.21-ResultPattern/
        ├── 4.22-OptionsPattern/
        ├── 4.23-DTO/
        ├── 4.24-PublishSubscribe/
        ├── 4.25-CacheAside/
        ├── 4.26-InboxPattern/
        ├── 4.27-AntiCorruptionLayer/
        ├── 4.28-ReadModel/
        ├── 4.29-RateLimiting/
        ├── 4.30-HealthEndpoint/
        └── 4.31-HostedService/
```

---

## How to Use This Repository

### 🎯 For Learning a New Pattern:

1. Navigate to the pattern's folder (e.g., `src/1-Creational/1.1-Singleton/`)
2. Read the pattern's `README.md` for:
   - **Intent**: What problem does it solve?
   - **When to use**: Real-world scenarios
   - **Benefits**: Why use this pattern?
   - **Drawbacks**: When NOT to use it
3. Study the implementation files — every file has detailed inline comments
4. Run the console demo to see it in action
5. Review the unit tests to understand expected behaviour

### 🔄 For Revisiting a Pattern:

1. Check `PROGRESS.md` to see implementation status
2. Jump directly to the pattern folder
3. Run the tests to see expected behaviour
4. Review code comments for a quick refresher

### 🏃 Running Examples:

```bash
# Run a specific pattern's demo
cd src/1-Creational/1.1-Singleton/SingletonPattern
dotnet run

# Run a specific pattern's tests
cd src/1-Creational/1.1-Singleton/SingletonPattern.Tests
dotnet test

# Run ALL tests across all implemented patterns (from repo root)
dotnet test DesignPatterns.slnx
```

### 🛠️ Building the Solution:

```bash
dotnet build DesignPatterns.slnx
```

---

## Progress Tracking

See [PROGRESS.md](PROGRESS.md) for detailed implementation status of all 57 patterns.

**Current status: 14 / 57 patterns implemented**

---

## Pattern Reference

### 1️⃣ Creational Patterns

| # | Pattern | Intent | Status |
|---|---------|--------|--------|
| 1.1 | **Singleton** | Ensure a class has only one instance and provide global access | ✅ Implemented |
| 1.2 | **Factory Method** | Define an interface for creating objects, but let subclasses decide which class to instantiate | ✅ Implemented |
| 1.3 | **Abstract Factory** | Provide an interface for creating families of related objects without specifying concrete classes | ✅ Implemented |
| 1.4 | **Builder** | Separate construction of a complex object from its representation | ✅ Implemented |
| 1.5 | **Prototype** | Create new objects by cloning an existing object (prototype) | ✅ Implemented |
| 1.6 | **Object Pool** | Reuse a fixed set of expensive objects instead of creating and destroying them on demand | ✅ Implemented |

### 2️⃣ Structural Patterns

| # | Pattern | Intent | Status |
|---|---------|--------|--------|
| 2.1 | **Adapter** | Convert the interface of a class into another interface clients expect | ✅ Implemented |
| 2.2 | **Bridge** | Decouple abstraction from implementation so both can vary independently | ✅ Implemented |
| 2.3 | **Composite** | Compose objects into tree structures to represent part-whole hierarchies | ✅ Implemented |
| 2.4 | **Decorator** | Attach additional responsibilities to an object dynamically | ✅ Implemented |
| 2.5 | **Facade** | Provide a unified interface to a set of interfaces in a subsystem | ✅ Implemented |
| 2.6 | **Flyweight** | Use sharing to support large numbers of fine-grained objects efficiently | ✅ Implemented |
| 2.7 | **Proxy** | Provide a surrogate or placeholder for another object to control access | ✅ Implemented |

### 3️⃣ Behavioral Patterns

| # | Pattern | Intent | Status |
|---|---------|--------|--------|
| 3.01 | **Chain of Responsibility** | Pass requests along a chain of handlers until one handles it | 🔜 Coming Soon |
| 3.02 | **Command** | Encapsulate a request as an object, allowing parameterization and queuing | 🔜 Coming Soon |
| 3.03 | **Interpreter** | Define a grammatical representation for a language and an interpreter | 🔜 Coming Soon |
| 3.04 | **Iterator** | Provide a way to access elements of a collection sequentially | 🔜 Coming Soon |
| 3.05 | **Mediator** | Define an object that encapsulates how a set of objects interact | 🔜 Coming Soon |
| 3.06 | **Memento** | Capture and restore an object's internal state without violating encapsulation | 🔜 Coming Soon |
| 3.07 | **Observer** | Define a one-to-many dependency so when one object changes, dependents are notified | 🔜 Coming Soon |
| 3.08 | **State** | Allow an object to alter its behavior when its internal state changes | 🔜 Coming Soon |
| 3.09 | **Strategy** | Define a family of algorithms and make them interchangeable | 🔜 Coming Soon |
| 3.10 | **Template Method** | Define the skeleton of an algorithm, deferring some steps to subclasses | 🔜 Coming Soon |
| 3.11 | **Visitor** | Represent an operation to be performed on elements of an object structure | 🔜 Coming Soon |
| 3.12 | **Null Object** | Provide a do-nothing default implementation to eliminate null checks | 🔜 Coming Soon |
| 3.13 | **Pipeline** | Pass data through a sequence of processing steps, each transforming and forwarding it | 🔜 Coming Soon |

### 4️⃣ Enterprise / Architectural Patterns

| # | Pattern | Intent | Status |
|---|---------|--------|--------|
| 4.01 | **Repository** | Mediate between domain and data mapping layers using a collection-like interface | ✅ Implemented |
| 4.02 | **Unit of Work** | Maintain a list of objects affected by a transaction and coordinate changes | 🔜 Coming Soon |
| 4.03 | **CQRS** | Separate read and write operations for better scalability and optimization | 🔜 Coming Soon |
| 4.04 | **Specification** | Encapsulate business rules that can be recombined | 🔜 Coming Soon |
| 4.05 | **Dependency Injection** | Inject dependencies rather than creating them internally | 🔜 Coming Soon |
| 4.06 | **Service Layer** | Define application's boundary with a layer of services | 🔜 Coming Soon |
| 4.07 | **Data Mapper** | Move data between objects and database while keeping them independent | 🔜 Coming Soon |
| 4.08 | **Active Record** | An object that wraps a row in a database table, encapsulating access | 🔜 Coming Soon |
| 4.09 | **Identity Map** | Ensure each object gets loaded only once by keeping every loaded object in a map | 🔜 Coming Soon |
| 4.10 | **Lazy Load** | Defer initialization of an object until it's needed | 🔜 Coming Soon |
| 4.11 | **Value Object** | Objects that are equal when their attributes are equal (no unique identity) | 🔜 Coming Soon |
| 4.12 | **Domain Event** | Capture domain occurrences that domain experts care about | 🔜 Coming Soon |
| 4.13 | **Aggregate Root** | Cluster of domain objects treated as a single unit with consistency boundaries | 🔜 Coming Soon |
| 4.14 | **Entity** | Objects with unique identity that runs through time and different states | 🔜 Coming Soon |
| 4.15 | **Event Sourcing** | Store state as a sequence of events rather than current state | 🔜 Coming Soon |
| 4.16 | **Circuit Breaker** | Prevent cascading failures by stopping calls to failing services | 🔜 Coming Soon |
| 4.17 | **Retry Pattern** | Handle transient failures by retrying failed operations | 🔜 Coming Soon |
| 4.18 | **Bulkhead** | Isolate resources to prevent total system failure | 🔜 Coming Soon |
| 4.19 | **Saga Pattern** | Manage distributed transactions across microservices | 🔜 Coming Soon |
| 4.20 | **Outbox Pattern** | Ensure reliable message/event publishing with database transactions | 🔜 Coming Soon |
| 4.21 | **Result Pattern** | Explicit success/failure handling without exceptions | 🔜 Coming Soon |
| 4.22 | **Options Pattern** | Strongly-typed access to groups of related settings (.NET specific) | 🔜 Coming Soon |
| 4.23 | **DTO** | Objects that carry data between processes to reduce method calls | 🔜 Coming Soon |
| 4.24 | **Publish-Subscribe** | Decouple publishers and subscribers through an event channel | 🔜 Coming Soon |
| 4.25 | **Cache-Aside** | Load data into cache on demand from the backing store; fall back on cache miss | 🔜 Coming Soon |
| 4.26 | **Inbox Pattern** | Idempotently consume incoming messages by recording them before processing | 🔜 Coming Soon |
| 4.27 | **Anti-Corruption Layer** | Translate between your domain model and an external or legacy system's model | 🔜 Coming Soon |
| 4.28 | **Read Model / Projection** | Maintain a denormalized, query-optimized view built from events or writes | 🔜 Coming Soon |
| 4.29 | **Rate Limiting / Throttle** | Constrain the rate of requests to protect services from overload | 🔜 Coming Soon |
| 4.30 | **Health Endpoint Monitoring** | Expose a health check endpoint for readiness and liveness probes | 🔜 Coming Soon |
| 4.31 | **Hosted Service / Background Worker** | Run long-lived background work within the .NET host lifecycle via IHostedService | 🔜 Coming Soon |

---

## Learning Path Recommendations

### 🌱 Beginner Path (Start here):
Singleton → Factory Method → Strategy → Repository → Result Pattern

These are foundational and you'll encounter them in almost every real-world codebase.

### 🌿 Intermediate Path:
Builder → Decorator → Observer → Unit of Work → CQRS

Common in modern .NET applications and API design.

### 🌳 Advanced Path:
Abstract Factory → Composite → Mediator → Event Sourcing → Saga Pattern

For complex enterprise systems and distributed architecture.

---

## Technologies Used

- **.NET 10** (latest)
- **C# 13**
- **xUnit** for unit testing

---

## Resources & References

- **Gang of Four (GoF)**: "Design Patterns: Elements of Reusable Object-Oriented Software"
- **Martin Fowler**: "Patterns of Enterprise Application Architecture"
- **Eric Evans**: "Domain-Driven Design"
- **Microsoft Docs**: .NET design patterns and best practices

---

## License

MIT License — feel free to use this for learning purposes.

---

**Happy Learning! 🚀**

*Last Updated: 2026-06-10*
