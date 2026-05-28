namespace AbstractFactoryPattern;

/// <summary>
/// CONCRETE FACTORY #1: Light Theme
///
/// Produces the full family of Light-themed components.
/// Every component it creates is visually consistent with the light style.
///
/// CRITICAL RULE: this factory ONLY creates Light components.
/// Mixing a LightButton with a DarkCheckbox would break visual consistency —
/// the factory prevents that by being the single source for an entire family.
/// </summary>
public class LightThemeFactory : IUIFactory
{
    public IButton CreateButton(string label) => new LightButton(label);
    public ICheckbox CreateCheckbox(string label) => new LightCheckbox(label);
}

/// <summary>
/// Concrete Product: Light-themed Button.
/// White background, dark text, subtle border shadow.
/// </summary>
public class LightButton : IButton
{
    private readonly string _label;

    public LightButton(string label) => _label = label;

    public string Theme => "Light";

    public void Render() =>
        Console.WriteLine($"  [LIGHT BUTTON ] ┌─────────────────┐");
    public void Click() =>
        Console.WriteLine($"  [LIGHT BUTTON ] '{_label}' clicked — light ripple effect");

    // Override to give a cleaner render that includes the label
    // (Render/Click split deliberately to mirror real UI component lifecycle)
    public void RenderWithLabel() =>
        Console.WriteLine($"  [LIGHT BUTTON ] │  {_label,-15}│  (white bg, dark text, shadow)");
}

/// <summary>
/// Concrete Product: Light-themed Checkbox.
/// Light border, subtle check mark styling.
/// </summary>
public class LightCheckbox : ICheckbox
{
    private readonly string _label;

    public LightCheckbox(string label) => _label = label;

    public string Theme => "Light";
    public bool IsChecked { get; private set; }

    public void Render() =>
        Console.WriteLine($"  [LIGHT CHECKBOX] [{(IsChecked ? "✓" : " ")}] {_label}  (light border, gray tick)");

    public void Toggle()
    {
        IsChecked = !IsChecked;
        Console.WriteLine($"  [LIGHT CHECKBOX] '{_label}' → {(IsChecked ? "checked ✓" : "unchecked")}");
    }
}
