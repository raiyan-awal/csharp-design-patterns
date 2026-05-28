namespace AbstractFactoryPattern;

/// <summary>
/// CLIENT — Uses the Abstract Factory without knowing any concrete types.
///
/// UIRenderer depends only on IUIFactory, IButton, and ICheckbox.
/// It has zero references to LightThemeFactory, DarkButton, etc.
///
/// This is the payoff of Abstract Factory: swap the factory at construction
/// time and the entire rendered UI switches theme — no other changes needed.
/// </summary>
public class UIRenderer
{
    private readonly IUIFactory _factory;

    /// <summary>
    /// The factory is injected — the renderer never decides which theme to use.
    /// That decision belongs to the caller (application startup, user preference, etc.)
    /// </summary>
    public UIRenderer(IUIFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Renders a login form using whatever theme the factory provides.
    /// Notice: no mention of "Light" or "Dark" anywhere in this method.
    /// </summary>
    public void RenderLoginForm()
    {
        Console.WriteLine("  ┌── Login Form ────────────────────────────────┐");

        var rememberMe = _factory.CreateCheckbox("Remember me");
        var loginButton = _factory.CreateButton("Login");
        var cancelButton = _factory.CreateButton("Cancel");

        rememberMe.Render();
        loginButton.Render();
        cancelButton.Render();

        Console.WriteLine("\n  Simulating user interaction...");
        rememberMe.Toggle();
        loginButton.Click();

        Console.WriteLine("  └──────────────────────────────────────────────┘");
    }

    /// <summary>
    /// Renders a settings panel. Same factory, different components — all consistent.
    /// </summary>
    public void RenderSettingsPanel()
    {
        Console.WriteLine("  ┌── Settings Panel ────────────────────────────┐");

        var darkModeToggle = _factory.CreateCheckbox("Dark mode");
        var notificationsToggle = _factory.CreateCheckbox("Enable notifications");
        var saveButton = _factory.CreateButton("Save Changes");
        var resetButton = _factory.CreateButton("Reset Defaults");

        darkModeToggle.Render();
        notificationsToggle.Render();
        saveButton.Render();
        resetButton.Render();

        Console.WriteLine("\n  Simulating interaction...");
        darkModeToggle.Toggle();
        notificationsToggle.Toggle();
        saveButton.Click();

        Console.WriteLine("  └──────────────────────────────────────────────┘");
    }
}
