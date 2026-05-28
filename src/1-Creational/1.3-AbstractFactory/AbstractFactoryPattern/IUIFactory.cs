namespace AbstractFactoryPattern;

/// <summary>
/// ABSTRACT FACTORY PATTERN - Abstract Factory Interface
///
/// IUIFactory is the "Abstract Factory" — it declares creation methods for each
/// distinct product type in the family (Button, Checkbox).
///
/// KEY CONCEPT: every method returns an INTERFACE, never a concrete class.
/// The caller never knows which concrete product it receives.
///
/// To add a new theme, you implement this interface — no existing code changes.
/// </summary>
public interface IUIFactory
{
    /// <summary>Creates a button styled for this theme.</summary>
    IButton CreateButton(string label);

    /// <summary>Creates a checkbox styled for this theme.</summary>
    ICheckbox CreateCheckbox(string label);
}

// ─────────────────────────────────────────────────────────────────────────────
// ABSTRACT PRODUCTS
// Interfaces that all concrete products (per theme) must implement.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Abstract Product A — defines what every button can do,
/// regardless of which theme created it.
/// </summary>
public interface IButton
{
    /// <summary>The theme family this component belongs to (e.g., "Light", "Dark").</summary>
    string Theme { get; }

    /// <summary>Renders the button to the output surface.</summary>
    void Render();

    /// <summary>Simulates a click interaction.</summary>
    void Click();
}

/// <summary>
/// Abstract Product B — defines what every checkbox can do,
/// regardless of which theme created it.
/// </summary>
public interface ICheckbox
{
    /// <summary>The theme family this component belongs to.</summary>
    string Theme { get; }

    /// <summary>Whether the checkbox is currently checked.</summary>
    bool IsChecked { get; }

    /// <summary>Renders the checkbox to the output surface.</summary>
    void Render();

    /// <summary>Flips the checked state.</summary>
    void Toggle();
}
