# Language & Framework-Specific Patterns

Patterns in this document are **not implemented** in this repository — they are either language-specific idioms that don't translate directly to C#, or framework conventions that only exist in their native ecosystem. They are documented here for reference and cross-language awareness.

---

## Python / Django / Flask / FastAPI

### Django

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **MTV (Model-Template-View)** | Django's MVC variant — Template replaces the View role, View replaces the Controller; separates data, logic, and presentation | Every Django project; `models.py`, `views.py`, `templates/` |
| **QuerySet / Lazy Evaluation** | Queries are objects that compose via chaining (`.filter().exclude().order_by()`) and only hit the DB when evaluated | Django ORM; similar in spirit to LINQ-to-SQL in .NET |
| **Django Middleware** | Class- or function-based handlers that wrap every request/response cycle for cross-cutting concerns | `MIDDLEWARE` setting; auth, CSRF, session, GZip are all built-in middleware |
| **Django Signals** | Built-in Observer — `pre_save`, `post_save`, `post_delete` fire automatically around model lifecycle events | Auditing, cache invalidation, async side effects without coupling models |
| **Class-Based Views (CBV)** | Compose reusable view logic through mixin inheritance (`LoginRequiredMixin`, `CreateView`) instead of repeating in functions | Generic views for CRUD; `ListView`, `DetailView`, `FormView` |
| **Django App** | Self-contained module with its own `models`, `views`, `urls`, `admin`, and `migrations`; plugged into a project's `INSTALLED_APPS` | How all Django projects are structured; reusable apps can be published to PyPI |
| **Django Form / ModelForm** | Encapsulate field validation, cleaning, and HTML rendering in one class; `ModelForm` auto-generates from a model | All user input handling; replaces manual request data parsing |
| **Django Admin** | Automatic CRUD UI generated from model definitions; customizable via `ModelAdmin` classes | Internal back-offices; rapid prototyping of management interfaces |

### Flask

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Application Factory** | Create the Flask app inside a `create_app()` function rather than at module level, enabling different configs per environment and easier testing | Standard Flask project layout; `flask --app myapp create_app` |
| **Blueprint** | Group related routes, templates, and static files into a reusable module; registered on the app at a URL prefix | Equivalent to Django apps; used to split large Flask apps into features |
| **Flask Extension / `init_app()`** | Third-party extensions (SQLAlchemy, Migrate, Login) accept `init_app(app)` so they work with the application factory pattern | Flask-SQLAlchemy, Flask-Login, Flask-Migrate |
| **Context Locals (`g`, `current_app`, `request`)** | Thread- or async-context-local proxies that give access to the current request and app without passing them explicitly | Every Flask request handler; `g` is per-request scratch space |

### FastAPI

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Dependency Injection via `Depends()`** | Declare dependencies as function parameters; FastAPI resolves and injects them automatically, supporting nesting and overrides | Database sessions, auth, config — the idiomatic FastAPI alternative to constructor DI |
| **Pydantic Model** | Combine schema definition, runtime validation, and serialization in one class; used as both request body and response model | All FastAPI endpoints; also used in data pipelines, settings, and CLI tools |
| **APIRouter** | Blueprint-like route grouping with shared prefix, tags, and dependencies; assembled into the main app | Structuring large FastAPI projects into feature modules |
| **Lifespan Events (`@asynccontextmanager`)** | Run startup and shutdown code (DB pool, ML model load) in a single async context manager rather than separate event handlers | FastAPI 0.93+; replaces `on_event("startup")` / `on_event("shutdown")` |

---

## Node.js / JavaScript

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **CommonJS Module** | Encapsulate private state and expose a controlled public API via `module.exports` | Every `.js` file before ESM; still dominant in Node.js backends |
| **Event Emitter** | Built-in pub/sub for async events; objects extend `EventEmitter` to emit and listen to named events | `stream`, `http`, `fs`, `net` — the entire Node.js core |
| **Express/Koa Middleware Chain** | `(req, res, next)` pipeline where each function either handles the request or passes it to the next handler | Express, Koa, Fastify, Hono |
| **Stream Pattern** | Composable readable / writable / transform / duplex streams with built-in backpressure handling | File I/O, HTTP responses, compression, encryption pipelines |
| **Worker Thread Pattern** | Offload CPU-intensive work to a thread pool without blocking the single-threaded event loop | `worker_threads` module; heavy computation, image processing |
| **NestJS Module + DI** | Angular-inspired hierarchical module system with decorator-based dependency injection (TypeScript-first) | NestJS applications; closest Node.js analogue to ASP.NET Core DI |
| **Callback Pattern** | Pass a function as the last argument to receive async results `(err, result)` — the original Node.js async style | Legacy Node.js APIs; largely replaced by Promises and async/await |

---

## Java / JEE / Spring

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **DAO (Data Access Object)** | Abstraction layer over raw persistence operations (SQL/ORM); predates Repository and is more CRUD-centric | Spring JDBC, JPA DAOs, pre-Spring-Data apps |
| **Service Locator** | Look up dependencies at runtime from a central registry instead of injecting them | Java EE JNDI lookups; widely considered an anti-pattern vs DI |
| **Front Controller** | Single entry point for all HTTP requests; dispatches to appropriate handlers | Spring `DispatcherServlet`, Jakarta EE Servlets |
| **Interceptor** | AOP-style handler that runs before/after method calls or HTTP requests without modifying the target | Spring AOP `@Aspect`, Jakarta EE interceptors, Spring MVC `HandlerInterceptor` |
| **Session Facade** | EJB pattern — group related business operations into a single coarse-grained remote interface to reduce network round trips | Enterprise JavaBeans (EJB); less relevant post-Spring |
| **Business Delegate** | Decouple the presentation layer from business/service layer; hide remote call complexity | JEE multi-tier apps; largely replaced by Spring's direct DI |
| **Transfer Object Assembler** | Build a composite Transfer Object (DTO) by aggregating data from multiple business objects in one call | JEE Service tier; reduces chatty remote calls |
| **Value List Handler** | Manage large query result sets server-side; return pages of results to clients | JEE apps without ORM pagination support |
| **Composite Entity** | Treat a graph of related JPA/EJB entities as a single coarse-grained entity with one lifecycle | EJB 2.x era; `@Embedded` and aggregate roots are modern equivalents |

---

## Go

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Functional Options** | Configure a struct using variadic option functions (`func(*Server)`) instead of telescoping constructors | gRPC-Go, `net/http`, most Go SDKs — the idiomatic Go alternative to named parameters or a config struct |
| **Embedding** | Compose behavior into a struct by embedding another type — Go's answer to inheritance | Standard library everywhere; `http.ResponseWriter` embeds, `sync.Mutex` embedding |
| **Interface Composition** | Combine small, focused interfaces into larger ones (`io.ReadWriter = io.Reader + io.Writer`) | Go standard library (`io`, `net`, `fmt` packages) |
| **Error Wrapping with `%w`** | Wrap errors with context using `fmt.Errorf("doing X: %w", err)`, unwrap with `errors.Is` / `errors.As` | Every Go service; replaces stack traces with explicit call chains |
| **Fan-out / Fan-in** | Distribute work across multiple goroutines (fan-out), then collect results through a single channel (fan-in) | Concurrent data processing, worker pools, parallel API calls |
| **Context Propagation** | Pass `context.Context` as the first argument to carry cancellation signals, deadlines, and request-scoped values | Every Go function that does I/O or calls another service |
| **Table-Driven Tests** | Define test cases as a slice of structs and iterate with a single `t.Run` loop | Go testing convention; built into how the standard `testing` package is used |

---

## Rust

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Newtype** | Wrap a primitive in a single-field struct (`struct UserId(u32)`) for type safety without runtime cost | Everywhere in idiomatic Rust to prevent mixing up `UserId` with `OrderId` |
| **Typestate** | Encode valid state transitions in the type system so the compiler rejects invalid operations at compile time | `builder` APIs, connection lifecycle (`Unconnected` → `Connected` → `Authenticated`) |
| **RAII (Resource Acquisition Is Initialization)** | Tie resource lifetime to scope; the `Drop` trait runs cleanup automatically when a value goes out of scope | Rust ownership system — `MutexGuard`, `File`, database connections |
| **Extension Trait** | Add methods to a type you don't own by defining a trait and implementing it for that type | `tokio::io::AsyncReadExt`, `itertools::Itertools` |
| **Error Handling with `?` + `thiserror`/`anyhow`** | Propagate errors ergonomically with `?`; use `thiserror` for library errors, `anyhow` for application errors | All serious Rust applications; replaces exception-based error flow |
| **Zero-Cost Abstraction** | Design philosophy: abstractions (iterators, traits, generics) compile down to code as efficient as hand-written loops | Rust iterators, `async/await` state machines, trait objects vs generics |

---

## PHP / Laravel

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Laravel Facade** | Provide a static proxy to services registered in the IoC container — **distinct from GoF Facade** | `Cache::get()`, `DB::table()`, `Auth::user()` in Laravel |
| **Service Container (IoC)** | Central registry that resolves dependencies automatically; binds interfaces to implementations | `app()->make()`, constructor injection in Laravel controllers and services |
| **Eloquent Active Record** | Each model class represents a DB table; instances wrap rows and expose save/find/delete directly — tightly couples model to DB | Laravel Eloquent; the standard ORM in PHP Laravel apps |
| **Form Request** | Encapsulate HTTP input validation and authorization logic into a dedicated request class | `php artisan make:request`; keeps controller methods slim |
| **Middleware (Laravel/Slim)** | HTTP request/response pipeline where each middleware can inspect, modify, or short-circuit the request | Laravel kernel middleware, Slim middleware stack |
| **Event / Listener** | Laravel's built-in event system — fire events and register listeners that can be queued automatically | `event(new OrderShipped($order))` with `ShouldQueue` listeners |

---

## Ruby on Rails

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Convention over Configuration** | The core Rails philosophy — assume file locations, class names, and DB column names by convention so you configure only exceptions | Every Rails project; `app/models/user.rb` auto-maps to `users` table without configuration |
| **Active Record (Rails)** | The original implementation: each model class = one DB table; instances wrap rows and expose associations, validations, and persistence directly | `ApplicationRecord` base class; the pattern your list (4.08) is named after |
| **RESTful Resource Routing** | `resources :orders` generates all 7 CRUD routes automatically; enforces REST conventions across the whole app | `config/routes.rb`; every Rails CRUD feature |
| **Rails Callbacks** | `before_save`, `after_create`, `around_destroy` — lifecycle hooks that run automatically around model persistence events | Model validation side effects, auditing, cache busting; considered an anti-pattern for complex logic (use Service Objects instead) |
| **Concerns (`ActiveSupport::Concern`)** | Mixins for sharing reusable behavior across models or controllers without inheritance; included with `include` | `app/models/concerns/`, `app/controllers/concerns/`; e.g., a `Taggable` concern shared by multiple models |
| **Rails Engine** | A self-contained mini Rails app (with its own models, routes, and views) that can be mounted inside a larger Rails app | Devise (auth engine), Sidekiq Web UI; like a deployable plugin |
| **Service Object** | Plain Ruby class in `app/services/` that encapsulates a single business operation; the community's escape valve for fat models | `PlaceOrderService.call(user:, cart:)` — keeps models thin |
| **Form Object** | Wraps multi-model form submissions or complex input validation behind a single object; replaces `accepts_nested_attributes_for` | `app/forms/`; e.g., a registration form that creates both User and Profile |
| **Query Object** | Encapsulates a complex ActiveRecord query scope in its own class; keeps models and controllers free of query logic | `app/queries/ActiveOrdersQuery.new.call` |
| **Decorator / Presenter** | Wraps a model to add view-specific formatting without polluting the model with presentation logic | Draper gem; `UserDecorator#full_name` |

---

## Elixir / Phoenix

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **GenServer (Actor Model)** | A generic server process that encapsulates state and handles messages sequentially; the fundamental building block of concurrent Elixir systems | OTP — every stateful background process, connection pool, cache |
| **Supervisor Tree** | Hierarchical process supervision where parent supervisors restart failed child processes; enables fault-tolerant "let it crash" design | OTP applications; Phoenix ships with a default supervision tree |
| **Context (Phoenix)** | Bounded module that groups related domain functions and is the only public API for a feature area; enforces boundaries without separate projects | `Accounts`, `Orders`, `Catalog` — Phoenix's answer to Service Layer + DDD bounded contexts |
| **LiveView** | Server-rendered HTML with real-time interactivity over WebSockets; the server holds all state and pushes diffs to the browser | Phoenix LiveView; eliminates client-side JS for many real-time UI features |
| **Plug** | Composable middleware specification — a function or module that transforms a `conn` struct and passes it to the next plug | Phoenix router pipeline; every HTTP concern (auth, logging, parsing) is a Plug |
| **Changeset** | A data structure that tracks and validates changes to a struct before persisting; separates validation from persistence | Ecto (Elixir's DB library); the functional alternative to Rails model validations |
| **Pipeline Composition (`|>`)** | The pipe operator chains transformations left-to-right, making data flow explicit; functions are the unit of composition, not objects | Idiomatic Elixir everywhere; replaces method chaining on objects |
| **ETS (Erlang Term Storage)** | In-memory key-value store built into the BEAM VM; used for shared state across processes without message passing | Caching, rate limiting counters, presence tracking — no Redis needed for simple cases |

---

## Kotlin / Ktor

| Pattern | Intent | Where You See It |
|---------|--------|-----------------|
| **Coroutine-Based Handler** | Every route handler is a suspending function; structured concurrency via coroutine scopes instead of reactive streams or thread pools | Ktor routing DSL; `call.respondText {}` runs in a coroutine automatically |
| **Application Plugin (Feature)** | Install cross-cutting behavior (auth, serialization, logging, CORS) by calling `install(Plugin)` in the application block | `install(ContentNegotiation)`, `install(Authentication)` — Ktor's equivalent to ASP.NET Core middleware |
| **Routing DSL** | Type-safe, nested route definition using Kotlin lambdas rather than annotations or reflection | `routing { get("/users") { ... } }` — composable and testable without a running server |
| **Kotlin Sealed Classes for Result** | Represent success/failure as a sealed class hierarchy (`sealed class Result<T>`); exhaustive `when` expressions force callers to handle both cases | Idiomatic alternative to exceptions in Kotlin services; equivalent to Result Pattern (4.21) |
| **Data Class + Copy** | Immutable value objects with auto-generated `equals`, `hashCode`, and `copy()`; `copy()` creates a modified clone | Kotlin DTOs, domain value objects; equivalent to C# records |
| **Extension Functions** | Add methods to existing classes without inheritance; used extensively to keep domain classes clean | `fun String.toSlug()`, `fun User.toDto()` — Kotlin's alternative to C# extension methods |
| **Companion Object** | A singleton attached to a class; holds factory methods and constants without static keyword | `User.of(...)`, `Order.empty()` — Kotlin's answer to `static` factory methods in Java/C# |
| **Koin / Kodein DI** | Kotlin-native DI frameworks using DSL-based module definitions instead of annotation processing | `val userRepo by inject<UserRepository>()` — lighter weight than Spring DI for Ktor apps |

---

## Cross-Language Notes

Some patterns in this document have direct conceptual equivalents in C# but use different idioms:

| This Document | C# / .NET Equivalent |
|---------------|----------------------|
| Go Functional Options | Options pattern (4.22) + builder fluent API |
| Rust Typestate | Generic type constraints + sealed classes |
| Java DAO | Repository (4.01) |
| Node.js Middleware Chain | Pipeline (3.13) / ASP.NET Core middleware |
| PHP Laravel Facade | Static gateway over DI — an anti-pattern in .NET; use DI directly |
| Java Session Facade | Service Layer (4.06) |
| Django QuerySet / Lazy Evaluation | LINQ-to-Entities (EF Core) — same deferred execution concept |
| Django Signals | Domain Events (4.12) / MediatR notifications |
| Django App | ASP.NET Core Area or class library project |
| Flask Application Factory | `WebApplication.CreateBuilder()` in ASP.NET Core |
| Flask Blueprint / FastAPI APIRouter | ASP.NET Core `MapGroup()` or area-based routing |
| FastAPI `Depends()` | ASP.NET Core constructor DI / `[FromServices]` |
| FastAPI Pydantic Model | C# record + FluentValidation or Data Annotations |
| Rails Active Record | Active Record (4.08) — same pattern, Rails is the canonical implementation |
| Rails Service / Form / Query Object | Service Layer (4.06), DTO (4.23), Specification (4.04) |
| Rails Concerns | C# partial classes or mixin-style extension methods |
| Rails Callbacks | Domain Events (4.12) — events are the safer, decoupled equivalent |
| Phoenix Context | Service Layer (4.06) + bounded context from DDD |
| Phoenix Plug | Pipeline (3.13) / ASP.NET Core middleware |
| Elixir Changeset | FluentValidation + Result Pattern (4.21) |
| Elixir GenServer | `BackgroundService` / Actor frameworks like Proto.Actor or Orleans |
| Kotlin Sealed Result | Result Pattern (4.21) |
| Kotlin Data Class + Copy | C# `record` with `with` expression |
| Kotlin Extension Functions | C# extension methods |
| Kotlin Companion Object | C# static factory methods |

---

*Last Updated: 2026-05-21 — Python (Django, Flask, FastAPI), Ruby on Rails, Elixir/Phoenix, Kotlin/Ktor added*
