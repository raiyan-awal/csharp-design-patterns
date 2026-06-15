using InterpreterPattern;

// ============================================================
// INTERPRETER PATTERN — DEMO
// ============================================================
// A boolean rule engine for access control.
// Each grammar rule (AND, OR, NOT, variable, literal) maps to
// one expression class. Rules are built as trees and evaluated
// against a Context — the same tree works for any user.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void Evaluate(string label, IExpression rule, Context ctx)
{
    bool result = rule.Evaluate(ctx);
    Console.WriteLine($"  {(result ? "✓ ALLOWED" : "✗ DENIED ")}  {label}");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║     INTERPRETER PATTERN — Access Control Rules       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Each grammar rule (AND / OR / NOT / variable / literal)");
Console.WriteLine("is one class. Rules compose into trees and evaluate");
Console.WriteLine("against a Context — the same tree, different users.");

Pause();

// ── DEMO 1: Terminal expressions ──────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Terminal expressions: Literal and Variable");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var ctx1 = new Context(new Dictionary<string, bool>
{
    ["isLoggedIn"]    = true,
    ["hasPermission"] = false,
});

Console.WriteLine("Context: isLoggedIn=true, hasPermission=false");
Console.WriteLine();

Evaluate("Literal(true)",        new LiteralExpression(true),      ctx1);
Evaluate("Literal(false)",       new LiteralExpression(false),     ctx1);
Evaluate("Variable(isLoggedIn)", new VariableExpression("isLoggedIn"),    ctx1);
Evaluate("Variable(hasPermission)", new VariableExpression("hasPermission"), ctx1);

Pause();

// ── DEMO 2: AND, OR, NOT ──────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Compound expressions: AND / OR / NOT");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

Console.WriteLine("Context: isLoggedIn=true, hasPermission=false");
Console.WriteLine();

IExpression isLoggedIn    = new VariableExpression("isLoggedIn");
IExpression hasPermission = new VariableExpression("hasPermission");

Evaluate("isLoggedIn AND hasPermission",  new AndExpression(isLoggedIn, hasPermission), ctx1);
Evaluate("isLoggedIn OR  hasPermission",  new OrExpression(isLoggedIn,  hasPermission), ctx1);
Evaluate("NOT hasPermission",             new NotExpression(hasPermission),             ctx1);
Evaluate("NOT isLoggedIn",               new NotExpression(isLoggedIn),                ctx1);

Pause();

// ── DEMO 3: Nested expression tree ───────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Nested tree: (isAdmin OR isModerator) AND NOT isBanned");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// Rule: (isAdmin OR isModerator) AND NOT isBanned
IExpression moderationRule = new AndExpression(
    new OrExpression(
        new VariableExpression("isAdmin"),
        new VariableExpression("isModerator")),
    new NotExpression(
        new VariableExpression("isBanned")));

Console.WriteLine("Rule: (isAdmin OR isModerator) AND NOT isBanned");
Console.WriteLine();

var users = new[]
{
    ("Admin (not banned)",       new Dictionary<string, bool> { ["isAdmin"] = true,  ["isModerator"] = false, ["isBanned"] = false }),
    ("Moderator (not banned)",   new Dictionary<string, bool> { ["isAdmin"] = false, ["isModerator"] = true,  ["isBanned"] = false }),
    ("Admin but banned",         new Dictionary<string, bool> { ["isAdmin"] = true,  ["isModerator"] = false, ["isBanned"] = true  }),
    ("Regular user (not banned)",new Dictionary<string, bool> { ["isAdmin"] = false, ["isModerator"] = false, ["isBanned"] = false }),
};

foreach (var (label, vars) in users)
    Evaluate(label, moderationRule, new Context(vars));

Pause();

// ── DEMO 4: Feature gate — same rule, different contexts ──────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Same rule tree evaluated against different users");
Console.WriteLine("         Rule: isPremium OR (isTrialUser AND NOT trialExpired)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// Rule: isPremium OR (isTrialUser AND NOT trialExpired)
IExpression featureGate = new OrExpression(
    new VariableExpression("isPremium"),
    new AndExpression(
        new VariableExpression("isTrialUser"),
        new NotExpression(new VariableExpression("trialExpired"))));

Console.WriteLine("Rule: isPremium OR (isTrialUser AND NOT trialExpired)");
Console.WriteLine();

var subscribers = new[]
{
    ("Premium subscriber",            new Dictionary<string, bool> { ["isPremium"] = true,  ["isTrialUser"] = false, ["trialExpired"] = false }),
    ("Active trial user",             new Dictionary<string, bool> { ["isPremium"] = false, ["isTrialUser"] = true,  ["trialExpired"] = false }),
    ("Expired trial user",            new Dictionary<string, bool> { ["isPremium"] = false, ["isTrialUser"] = true,  ["trialExpired"] = true  }),
    ("Free user (no trial, no sub)",  new Dictionary<string, bool> { ["isPremium"] = false, ["isTrialUser"] = false, ["trialExpired"] = false }),
};

foreach (var (label, vars) in subscribers)
    Evaluate(label, featureGate, new Context(vars));

Pause();

// ── DEMO 5: Dynamic context — same user, changing state ───────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Dynamic context: same rule, same user, state changes");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// Rule: isLoggedIn AND hasPermission AND NOT accountLocked
IExpression accessRule = new AndExpression(
    new AndExpression(
        new VariableExpression("isLoggedIn"),
        new VariableExpression("hasPermission")),
    new NotExpression(new VariableExpression("accountLocked")));

var dynCtx = new Context(new Dictionary<string, bool>
{
    ["isLoggedIn"]    = false,
    ["hasPermission"] = true,
    ["accountLocked"] = false,
});

Console.WriteLine("Rule: isLoggedIn AND hasPermission AND NOT accountLocked");
Console.WriteLine();

Console.WriteLine("Before login:         isLoggedIn=false");
Evaluate("Access", accessRule, dynCtx);

dynCtx.Set("isLoggedIn", true);
Console.WriteLine();
Console.WriteLine("After login:          isLoggedIn=true");
Evaluate("Access", accessRule, dynCtx);

dynCtx.Set("accountLocked", true);
Console.WriteLine();
Console.WriteLine("Account locked:       accountLocked=true");
Evaluate("Access", accessRule, dynCtx);

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • IExpression         — one Evaluate(context) method");
Console.WriteLine("  • LiteralExpression   — terminal; always returns true or false");
Console.WriteLine("  • VariableExpression  — terminal; looks up name in Context");
Console.WriteLine("  • AndExpression       — non-terminal; both sides must be true");
Console.WriteLine("  • OrExpression        — non-terminal; either side must be true");
Console.WriteLine("  • NotExpression       — non-terminal; inverts its operand");
Console.WriteLine("  • Context             — variable bindings; mutable at runtime");
Console.WriteLine("  • Same rule tree → re-evaluated against any context");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
