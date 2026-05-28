namespace AbstractFactoryPattern;

/// <summary>
/// CONCRETE FACTORY #2: Dark Theme
///
/// Produces the full family of Dark-themed components.
/// Swapping LightThemeFactory for DarkThemeFactory in the client
/// re-themes the entire UI without changing a single line of client code.
/// </summary>
public class DarkThemeFactory : IUIFactory
{
    public IButton CreateButton(string label) => new DarkButton(label);
    public ICheckbox CreateCheckbox(string label) => new DarkCheckbox(label);
}

/// <summary>
/// Concrete Product: Dark-themed Button.
/// Dark background, light text, glowing shadow.
/// </summary>
public class DarkButton : IButton
{
    private readonly string _label;

    public DarkButton(string label) => _label = label;

    public string Theme => "Dark";

    public void Render() =>
        Console.WriteLine($"  [DARK  BUTTON ] ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");

    public void Click() =>
        Console.WriteLine($"  [DARK  BUTTON ] '{_label}' clicked — dark ripple + glow effect");
}

/// <summary>
/// Concrete Product: Dark-themed Checkbox.
/// Dark border, bright check mark on selection.
/// </summary>
public class DarkCheckbox : ICheckbox
{
    private readonly string _label;

    public DarkCheckbox(string label) => _label = label;

    public string Theme => "Dark";
    public bool IsChecked { get; private set; }

    public void Render() =>
        Console.WriteLine($"  [DARK  CHECKBOX] [{(IsChecked ? "✓" : " ")}] {_label}  (dark border, bright tick on check)");

    public void Toggle()
    {
        IsChecked = !IsChecked;
        Console.WriteLine($"  [DARK  CHECKBOX] '{_label}' → {(IsChecked ? "checked ✓" : "unchecked")}");
    }
}
