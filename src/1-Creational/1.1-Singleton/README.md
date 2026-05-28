# Singleton Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Ensure a class has only **one instance** and provide a **global point of access** to that instance.

## 🤔 Problem
Sometimes you need to ensure that a class has exactly one instance. For example:
- You want only one configuration manager for your application
- You need a single logger instance to avoid file conflicts
- You want to control access to a shared resource (database connection pool, cache, etc.)

Creating multiple instances of these classes would:
- Waste memory
- Cause inconsistent state
- Lead to conflicts (e.g., multiple loggers writing to the same file)

## ✅ Solution
The Singleton pattern solves this by:
1. Making the constructor **private** (prevents external instantiation with `new`)
2. Providing a **static property** that returns the single instance
3. Creating the instance **lazily** (only when first accessed) or **eagerly** (at class load time)
4. Ensuring **thread-safety** (multiple threads can't create multiple instances)

## 🏗️ Structure

```
┌─────────────────────────┐
│    Singleton            │
├─────────────────────────┤
│ - instance: Singleton   │  ← Private static field
│ - Singleton()           │  ← Private constructor
├─────────────────────────┤
│ + Instance: Singleton   │  ← Public static property
│ + SomeMethod()          │  ← Business methods
└─────────────────────────┘
```

## 💻 Implementation in This Example

### Files:
- **ConfigurationManager.cs** - Main singleton implementation (lazy, thread-safe using `Lazy<T>`)
- **EagerSingleton.cs** - Alternative implementation (eager initialization)
- **UnsafeSingleton.cs** - Anti-pattern example (NOT thread-safe - for educational purposes only)
- **Program.cs** - Demonstrations and usage examples

### Key Implementation Points:

1. **Private Constructor:**
   ```csharp
   private ConfigurationManager() { ... }
   ```
   Prevents `new ConfigurationManager()` from being called outside the class.

2. **Lazy Initialization with Thread-Safety:**
   ```csharp
   private static readonly Lazy<ConfigurationManager> _instance =
       new Lazy<ConfigurationManager>(() => new ConfigurationManager());
   ```
   - `Lazy<T>` ensures the instance is created only when first accessed
   - Thread-safe by default (no locks needed)
   - Best practice for .NET applications

3. **Public Static Property:**
   ```csharp
   public static ConfigurationManager Instance => _instance.Value;
   ```
   - Global access point
   - Returns the single instance

## 🚀 How to Run

```bash
cd src/1-Creational/1.1-Singleton/SingletonPattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.1-Singleton/SingletonPattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **Single Instance Creation** - Only one instance is created no matter how many times you access it
2. **Same Instance Reference** - All variables point to the exact same object in memory
3. **Shared State** - Changes made through one reference are visible through all references
4. **Thread-Safety** - Multiple threads can safely access the singleton simultaneously
5. **Eager vs Lazy** - Comparison of different initialization strategies

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Controlled Access** | Strict control over how and when the instance is created |
| **Memory Efficiency** | Only one instance exists in memory |
| **Global Access** | Easy access from anywhere in the application |
| **Lazy Initialization** | Instance created only when needed (saves resources) |
| **Thread-Safe** | Safe to use in multi-threaded environments |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **Testing Difficulty** | Hard to mock in unit tests (prefer dependency injection) |
| **Hidden Dependencies** | Not obvious from constructor what the class depends on |
| **Violates SRP** | Class manages both its own creation AND business logic |
| **Global State** | Can lead to tight coupling across the application |
| **Not Suitable for DI** | Conflicts with dependency injection principles |

## 🎓 When to Use

✅ **Good Candidates:**
- Configuration managers
- Logger instances
- Caching mechanisms
- Database connection pools
- Thread pools
- Hardware interface access (printer spooler, file system manager)

❌ **Bad Candidates:**
- Classes that need multiple instances with different configurations
- Classes with mutable state that varies based on input
- Anything that needs to be unit tested in isolation
- When dependency injection is already in use

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Dependency Injection** | When you need testability, flexibility, and loose coupling |
| **Static Class** | When you only need utility methods with no state |
| **Factory Pattern** | When you need to control object creation but allow multiple instances |

## 📚 Related Patterns

- **Factory Method** - Can use Singleton to ensure factory has only one instance
- **Abstract Factory** - Factories are often implemented as Singletons
- **Facade** - Facade objects are often Singletons

## 🔑 Key Takeaways

1. **Only use Singleton when you truly need exactly ONE instance**
2. **Prefer Dependency Injection for most scenarios** (better testability)
3. **Use `Lazy<T>` in .NET for thread-safe lazy initialization**
4. **Make the constructor private** (this is the core mechanism)
5. **Be aware of the drawbacks** (testing, global state, coupling)

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four)
- [Microsoft Docs: Singleton Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/singleton)
- [Dependency Injection vs Singleton](https://stackoverflow.com/questions/11371816/ioc-di-why-do-i-have-to-reference-all-layers-assemblies-in-application-s)

---

**Next Pattern:** [1.2 - Factory Method](../1.2-FactoryMethod/) →
