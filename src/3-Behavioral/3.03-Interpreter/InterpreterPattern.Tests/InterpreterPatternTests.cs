using InterpreterPattern;

namespace InterpreterPattern.Tests;

// ── Context ───────────────────────────────────────────────────

public class ContextTests
{
    [Fact]
    public void Lookup_ReturnsValue_WhenVariableDefined()
    {
        var ctx = new Context(new Dictionary<string, bool> { ["x"] = true });
        Assert.True(ctx.Lookup("x"));
    }

    [Fact]
    public void Lookup_ThrowsKeyNotFoundException_WhenVariableUndefined()
    {
        var ctx = new Context();
        Assert.Throws<KeyNotFoundException>(() => ctx.Lookup("missing"));
    }

    [Fact]
    public void Set_AddsVariable_LookupReturnsIt()
    {
        var ctx = new Context();
        ctx.Set("y", false);
        Assert.False(ctx.Lookup("y"));
    }

    [Fact]
    public void Set_OverwritesExistingVariable()
    {
        var ctx = new Context(new Dictionary<string, bool> { ["x"] = true });
        ctx.Set("x", false);
        Assert.False(ctx.Lookup("x"));
    }
}

// ── LiteralExpression ─────────────────────────────────────────

public class LiteralExpressionTests
{
    [Fact]
    public void Evaluate_True_ReturnsTrue()
    {
        var expr = new LiteralExpression(true);
        Assert.True(expr.Evaluate(new Context()));
    }

    [Fact]
    public void Evaluate_False_ReturnsFalse()
    {
        var expr = new LiteralExpression(false);
        Assert.False(expr.Evaluate(new Context()));
    }

    [Fact]
    public void Evaluate_IgnoresContext()
    {
        var expr = new LiteralExpression(true);
        var ctx  = new Context(new Dictionary<string, bool> { ["x"] = false });
        Assert.True(expr.Evaluate(ctx));
    }
}

// ── VariableExpression ────────────────────────────────────────

public class VariableExpressionTests
{
    [Fact]
    public void Evaluate_ReturnsTrue_WhenVariableIsTrue()
    {
        var ctx  = new Context(new Dictionary<string, bool> { ["flag"] = true });
        var expr = new VariableExpression("flag");
        Assert.True(expr.Evaluate(ctx));
    }

    [Fact]
    public void Evaluate_ReturnsFalse_WhenVariableIsFalse()
    {
        var ctx  = new Context(new Dictionary<string, bool> { ["flag"] = false });
        var expr = new VariableExpression("flag");
        Assert.False(expr.Evaluate(ctx));
    }

    [Fact]
    public void Evaluate_Throws_WhenVariableNotInContext()
    {
        var expr = new VariableExpression("undefined");
        Assert.Throws<KeyNotFoundException>(() => expr.Evaluate(new Context()));
    }

    [Fact]
    public void Evaluate_ReflectsContextChange_AfterSet()
    {
        var ctx  = new Context(new Dictionary<string, bool> { ["x"] = false });
        var expr = new VariableExpression("x");

        Assert.False(expr.Evaluate(ctx));
        ctx.Set("x", true);
        Assert.True(expr.Evaluate(ctx));
    }
}

// ── AndExpression ─────────────────────────────────────────────

public class AndExpressionTests
{
    private static IExpression T => new LiteralExpression(true);
    private static IExpression F => new LiteralExpression(false);
    private static Context     C => new();

    [Fact] public void TrueAndTrue_ReturnsTrue()   => Assert.True(new AndExpression(T, T).Evaluate(C));
    [Fact] public void TrueAndFalse_ReturnsFalse()  => Assert.False(new AndExpression(T, F).Evaluate(C));
    [Fact] public void FalseAndTrue_ReturnsFalse()  => Assert.False(new AndExpression(F, T).Evaluate(C));
    [Fact] public void FalseAndFalse_ReturnsFalse() => Assert.False(new AndExpression(F, F).Evaluate(C));
}

// ── OrExpression ──────────────────────────────────────────────

public class OrExpressionTests
{
    private static IExpression T => new LiteralExpression(true);
    private static IExpression F => new LiteralExpression(false);
    private static Context     C => new();

    [Fact] public void TrueOrTrue_ReturnsTrue()   => Assert.True(new OrExpression(T, T).Evaluate(C));
    [Fact] public void TrueOrFalse_ReturnsTrue()  => Assert.True(new OrExpression(T, F).Evaluate(C));
    [Fact] public void FalseOrTrue_ReturnsTrue()  => Assert.True(new OrExpression(F, T).Evaluate(C));
    [Fact] public void FalseOrFalse_ReturnsFalse() => Assert.False(new OrExpression(F, F).Evaluate(C));
}

// ── NotExpression ─────────────────────────────────────────────

public class NotExpressionTests
{
    private static Context C => new();

    [Fact]
    public void NotTrue_ReturnsFalse()
        => Assert.False(new NotExpression(new LiteralExpression(true)).Evaluate(C));

    [Fact]
    public void NotFalse_ReturnsTrue()
        => Assert.True(new NotExpression(new LiteralExpression(false)).Evaluate(C));
}

// ── Composite expression trees ────────────────────────────────

public class CompositeExpressionTests
{
    private static Context Ctx(params (string name, bool value)[] vars)
    {
        var dict = vars.ToDictionary(v => v.name, v => v.value);
        return new Context(dict);
    }

    [Fact]
    public void ModerationRule_AdminNotBanned_ReturnsTrue()
    {
        // (isAdmin OR isModerator) AND NOT isBanned
        IExpression rule = new AndExpression(
            new OrExpression(
                new VariableExpression("isAdmin"),
                new VariableExpression("isModerator")),
            new NotExpression(new VariableExpression("isBanned")));

        Assert.True(rule.Evaluate(
            Ctx(("isAdmin", true), ("isModerator", false), ("isBanned", false))));
    }

    [Fact]
    public void ModerationRule_AdminBanned_ReturnsFalse()
    {
        IExpression rule = new AndExpression(
            new OrExpression(
                new VariableExpression("isAdmin"),
                new VariableExpression("isModerator")),
            new NotExpression(new VariableExpression("isBanned")));

        Assert.False(rule.Evaluate(
            Ctx(("isAdmin", true), ("isModerator", false), ("isBanned", true))));
    }

    [Fact]
    public void ModerationRule_RegularUserNotBanned_ReturnsFalse()
    {
        IExpression rule = new AndExpression(
            new OrExpression(
                new VariableExpression("isAdmin"),
                new VariableExpression("isModerator")),
            new NotExpression(new VariableExpression("isBanned")));

        Assert.False(rule.Evaluate(
            Ctx(("isAdmin", false), ("isModerator", false), ("isBanned", false))));
    }

    [Fact]
    public void FeatureGate_PremiumUser_ReturnsTrue()
    {
        // isPremium OR (isTrialUser AND NOT trialExpired)
        IExpression rule = new OrExpression(
            new VariableExpression("isPremium"),
            new AndExpression(
                new VariableExpression("isTrialUser"),
                new NotExpression(new VariableExpression("trialExpired"))));

        Assert.True(rule.Evaluate(
            Ctx(("isPremium", true), ("isTrialUser", false), ("trialExpired", false))));
    }

    [Fact]
    public void FeatureGate_ActiveTrialUser_ReturnsTrue()
    {
        IExpression rule = new OrExpression(
            new VariableExpression("isPremium"),
            new AndExpression(
                new VariableExpression("isTrialUser"),
                new NotExpression(new VariableExpression("trialExpired"))));

        Assert.True(rule.Evaluate(
            Ctx(("isPremium", false), ("isTrialUser", true), ("trialExpired", false))));
    }

    [Fact]
    public void FeatureGate_ExpiredTrialUser_ReturnsFalse()
    {
        IExpression rule = new OrExpression(
            new VariableExpression("isPremium"),
            new AndExpression(
                new VariableExpression("isTrialUser"),
                new NotExpression(new VariableExpression("trialExpired"))));

        Assert.False(rule.Evaluate(
            Ctx(("isPremium", false), ("isTrialUser", true), ("trialExpired", true))));
    }

    [Fact]
    public void SameRuleTree_EvaluatedAgainstMultipleContexts_ReturnsCorrectResults()
    {
        IExpression rule = new AndExpression(
            new VariableExpression("a"),
            new VariableExpression("b"));

        Assert.True(rule.Evaluate(Ctx(("a", true),  ("b", true))));
        Assert.False(rule.Evaluate(Ctx(("a", true),  ("b", false))));
        Assert.False(rule.Evaluate(Ctx(("a", false), ("b", true))));
    }
}
