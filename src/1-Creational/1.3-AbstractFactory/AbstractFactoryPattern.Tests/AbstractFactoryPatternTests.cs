using AbstractFactoryPattern;

namespace AbstractFactoryPattern.Tests;

/// <summary>
/// Unit tests for the Abstract Factory pattern implementation.
///
/// These tests verify that:
/// 1. Each factory produces products of the correct theme
/// 2. All products from one factory share the same theme (family consistency)
/// 3. Client code (UIRenderer) works identically with any factory
/// 4. Products behave correctly (checkbox toggling, initial state)
/// 5. Factories are interchangeable without changing client code
/// </summary>
public class AbstractFactoryPatternTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // LIGHT THEME FACTORY
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LightThemeFactory_CreateButton_ReturnsLightThemeButton()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var button = factory.CreateButton("OK");

        // Assert
        Assert.Equal("Light", button.Theme);
    }

    [Fact]
    public void LightThemeFactory_CreateCheckbox_ReturnsLightThemeCheckbox()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var checkbox = factory.CreateCheckbox("Remember me");

        // Assert
        Assert.Equal("Light", checkbox.Theme);
    }

    [Fact]
    public void LightThemeFactory_AllProducts_BelongToSameFamily()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var button = factory.CreateButton("Submit");
        var checkbox = factory.CreateCheckbox("Accept terms");

        // Assert — family consistency: all products share the same theme
        Assert.Equal(button.Theme, checkbox.Theme);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DARK THEME FACTORY
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DarkThemeFactory_CreateButton_ReturnsDarkThemeButton()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();

        // Act
        var button = factory.CreateButton("OK");

        // Assert
        Assert.Equal("Dark", button.Theme);
    }

    [Fact]
    public void DarkThemeFactory_CreateCheckbox_ReturnsDarkThemeCheckbox()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();

        // Act
        var checkbox = factory.CreateCheckbox("Remember me");

        // Assert
        Assert.Equal("Dark", checkbox.Theme);
    }

    [Fact]
    public void DarkThemeFactory_AllProducts_BelongToSameFamily()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();

        // Act
        var button = factory.CreateButton("Submit");
        var checkbox = factory.CreateCheckbox("Accept terms");

        // Assert
        Assert.Equal(button.Theme, checkbox.Theme);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FAMILY ISOLATION — products from different factories must differ
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LightFactory_And_DarkFactory_ProduceDifferentThemes()
    {
        // Arrange
        IUIFactory lightFactory = new LightThemeFactory();
        IUIFactory darkFactory = new DarkThemeFactory();

        // Act
        var lightButton = lightFactory.CreateButton("OK");
        var darkButton = darkFactory.CreateButton("OK");

        // Assert — same interface, different theme families
        Assert.NotEqual(lightButton.Theme, darkButton.Theme);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRODUCT BEHAVIOUR — Checkbox
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Checkbox_InitialState_IsUnchecked()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var checkbox = factory.CreateCheckbox("Remember me");

        // Assert
        Assert.False(checkbox.IsChecked);
    }

    [Fact]
    public void Checkbox_AfterOneToggle_IsChecked()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();
        var checkbox = factory.CreateCheckbox("Remember me");

        // Act
        checkbox.Toggle();

        // Assert
        Assert.True(checkbox.IsChecked);
    }

    [Fact]
    public void Checkbox_AfterTwoToggles_ReturnToUnchecked()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();
        var checkbox = factory.CreateCheckbox("Dark mode");

        // Act
        checkbox.Toggle();
        checkbox.Toggle();

        // Assert
        Assert.False(checkbox.IsChecked);
    }

    [Fact]
    public void Checkbox_ToggleBehaviour_IsConsistentAcrossThemes()
    {
        // Arrange
        IUIFactory lightFactory = new LightThemeFactory();
        IUIFactory darkFactory = new DarkThemeFactory();

        var lightCheckbox = lightFactory.CreateCheckbox("Option");
        var darkCheckbox = darkFactory.CreateCheckbox("Option");

        // Act
        lightCheckbox.Toggle();
        darkCheckbox.Toggle();

        // Assert — different themes, identical toggle behaviour
        Assert.Equal(lightCheckbox.IsChecked, darkCheckbox.IsChecked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRODUCT BEHAVIOUR — Button
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Button_IsCreated_WithCorrectInterface()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var button = factory.CreateButton("Save");

        // Assert — returned object implements the IButton contract
        Assert.IsAssignableFrom<IButton>(button);
    }

    [Fact]
    public void Checkbox_IsCreated_WithCorrectInterface()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();

        // Act
        var checkbox = factory.CreateCheckbox("Enable");

        // Assert — returned object implements the ICheckbox contract
        Assert.IsAssignableFrom<ICheckbox>(checkbox);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INTERCHANGEABILITY — UIRenderer works with any factory
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UIRenderer_WithLightFactory_DoesNotThrow()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();
        var renderer = new UIRenderer(factory);

        // Act & Assert — no exceptions thrown; renderer works with this factory
        var exception = Record.Exception(() =>
        {
            renderer.RenderLoginForm();
            renderer.RenderSettingsPanel();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void UIRenderer_WithDarkFactory_DoesNotThrow()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();
        var renderer = new UIRenderer(factory);

        // Act & Assert — same renderer, different factory, no exceptions
        var exception = Record.Exception(() =>
        {
            renderer.RenderLoginForm();
            renderer.RenderSettingsPanel();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void UIRenderer_IsInterchangeable_BetweenFactories()
    {
        // Arrange — two renderers, two factories, same client code
        var lightRenderer = new UIRenderer(new LightThemeFactory());
        var darkRenderer = new UIRenderer(new DarkThemeFactory());

        // Act & Assert — both run without error; client code is identical
        var ex1 = Record.Exception(() => lightRenderer.RenderLoginForm());
        var ex2 = Record.Exception(() => darkRenderer.RenderLoginForm());

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MULTIPLE INSTANCES — factory creates fresh products each time
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Factory_CreateButton_ReturnsDifferentInstancesEachCall()
    {
        // Arrange
        IUIFactory factory = new LightThemeFactory();

        // Act
        var button1 = factory.CreateButton("OK");
        var button2 = factory.CreateButton("OK");

        // Assert — each call produces a new object (not a shared singleton)
        Assert.NotSame(button1, button2);
    }

    [Fact]
    public void Factory_CreateCheckbox_ReturnsDifferentInstancesEachCall()
    {
        // Arrange
        IUIFactory factory = new DarkThemeFactory();

        // Act
        var chk1 = factory.CreateCheckbox("Accept");
        var chk2 = factory.CreateCheckbox("Accept");

        // Assert — toggling one should not affect the other
        chk1.Toggle();
        Assert.True(chk1.IsChecked);
        Assert.False(chk2.IsChecked);
    }
}
