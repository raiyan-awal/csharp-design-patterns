using SingletonPattern;

namespace SingletonPattern.Tests;

/// <summary>
/// Unit tests for the Singleton pattern implementations.
///
/// These tests verify that:
/// 1. Only one instance is created across the application
/// 2. All references point to the same instance
/// 3. State is shared across all accesses
/// 4. Thread-safety is maintained in concurrent scenarios
/// 5. Instance survives across multiple access points
/// </summary>
public class SingletonPatternTests
{
    /// <summary>
    /// TEST 1: Verify that accessing Instance multiple times returns the same instance.
    ///
    /// This is the core behavior of the Singleton pattern:
    /// No matter how many times you call ConfigurationManager.Instance,
    /// you should always get back the exact same object in memory.
    /// </summary>
    [Fact]
    public void Instance_WhenAccessedMultipleTimes_ReturnsSameInstance()
    {
        // Arrange & Act
        // Access the singleton instance multiple times
        var instance1 = ConfigurationManager.Instance;
        var instance2 = ConfigurationManager.Instance;
        var instance3 = ConfigurationManager.Instance;

        // Assert
        // All three variables should point to the exact same object in memory
        // ReferenceEquals checks if two variables reference the same object
        Assert.True(ReferenceEquals(instance1, instance2));
        Assert.True(ReferenceEquals(instance2, instance3));
        Assert.True(ReferenceEquals(instance1, instance3));

        // Alternative assertion using Assert.Same (does the same check)
        Assert.Same(instance1, instance2);
        Assert.Same(instance2, instance3);
    }

    /// <summary>
    /// TEST 2: Verify that state changes are visible across all references.
    ///
    /// Since there's only one instance, any modifications made through one
    /// reference should be immediately visible through all other references.
    /// </summary>
    [Fact]
    public void Instance_WhenStateIsModified_ChangesAreVisibleAcrossAllReferences()
    {
        // Arrange
        // Get two references to the singleton
        var config1 = ConfigurationManager.Instance;
        var config2 = ConfigurationManager.Instance;

        // Act
        // Modify a setting through the first reference
        config1.SetSetting("TestKey", "TestValue");

        // Assert
        // The change should be visible through the second reference
        // because they both point to the same object
        var valueFromConfig2 = config2.GetSetting("TestKey");
        Assert.Equal("TestValue", valueFromConfig2);
    }

    /// <summary>
    /// TEST 3: Verify that the singleton has default settings on first access.
    ///
    /// This tests that the singleton is initialized properly with expected default values.
    /// </summary>
    [Fact]
    public void Instance_OnFirstAccess_HasDefaultSettings()
    {
        // Arrange & Act
        var config = ConfigurationManager.Instance;

        // Assert
        // The singleton should have the default settings defined in its constructor
        Assert.Equal("Design Patterns Demo", config.GetSetting("AppName"));
        Assert.Equal("1.0.0", config.GetSetting("Version"));
        Assert.Equal("Development", config.GetSetting("Environment"));
    }

    /// <summary>
    /// TEST 4: Verify that non-existent settings return null.
    ///
    /// This tests the behavior when trying to get a setting that doesn't exist.
    /// </summary>
    [Fact]
    public void GetSetting_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var config = ConfigurationManager.Instance;

        // Act
        var value = config.GetSetting("NonExistentKey");

        // Assert
        Assert.Null(value);
    }

    /// <summary>
    /// TEST 5: Verify that SetSetting updates existing values.
    ///
    /// This tests that calling SetSetting with an existing key updates
    /// the value rather than creating a duplicate entry.
    /// </summary>
    [Fact]
    public void SetSetting_WhenKeyAlreadyExists_UpdatesValue()
    {
        // Arrange
        var config = ConfigurationManager.Instance;

        // Act
        // Set an initial value
        config.SetSetting("UpdateTest", "InitialValue");

        // Update the same key with a new value
        config.SetSetting("UpdateTest", "UpdatedValue");

        // Assert
        // The key should now have the updated value
        Assert.Equal("UpdatedValue", config.GetSetting("UpdateTest"));
    }

    /// <summary>
    /// TEST 6: Verify thread-safety of the Singleton pattern.
    ///
    /// This test creates multiple threads that all try to access the singleton
    /// instance simultaneously. Despite concurrent access, only ONE instance
    /// should be created, and all threads should receive the same instance.
    ///
    /// This is a critical test because in multi-threaded environments,
    /// improper singleton implementation can create multiple instances (race condition).
    /// </summary>
    [Fact]
    public void Instance_WhenAccessedConcurrently_ReturnsSameInstanceAcrossAllThreads()
    {
        // Arrange
        const int threadCount = 10;
        var instances = new ConfigurationManager[threadCount];

        // Act
        // Create multiple threads that all access the singleton
        var threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            int index = i; // Capture loop variable
            threads[i] = new Thread(() =>
            {
                // Each thread gets the singleton instance
                instances[index] = ConfigurationManager.Instance;

                // Small delay to increase chance of race condition (if implementation is broken)
                Thread.Sleep(10);
            });
        }

        // Start all threads
        foreach (var thread in threads)
        {
            thread.Start();
        }

        // Wait for all threads to complete
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        // All instances collected by all threads should be the same object
        var firstInstance = instances[0];

        for (int i = 1; i < threadCount; i++)
        {
            Assert.Same(firstInstance, instances[i]);
        }
    }

    /// <summary>
    /// TEST 7: Verify that GetAllSettings returns all current settings.
    ///
    /// This tests the method that retrieves all settings as a dictionary.
    /// </summary>
    [Fact]
    public void GetAllSettings_ReturnsAllCurrentSettings()
    {
        // Arrange
        var config = ConfigurationManager.Instance;

        // Add some test settings
        config.SetSetting("TestKey1", "Value1");
        config.SetSetting("TestKey2", "Value2");

        // Act
        var allSettings = config.GetAllSettings();

        // Assert
        // Should contain at least the test settings we just added
        Assert.Contains("TestKey1", allSettings.Keys);
        Assert.Contains("TestKey2", allSettings.Keys);
        Assert.Equal("Value1", allSettings["TestKey1"]);
        Assert.Equal("Value2", allSettings["TestKey2"]);
    }

    /// <summary>
    /// TEST 8: Verify that the returned settings dictionary is read-only.
    ///
    /// This ensures that external code cannot modify the internal settings
    /// dictionary directly - they must use SetSetting instead.
    /// </summary>
    [Fact]
    public void GetAllSettings_ReturnsReadOnlyDictionary()
    {
        // Arrange
        var config = ConfigurationManager.Instance;

        // Act
        var allSettings = config.GetAllSettings();

        // Assert
        // The returned dictionary should be IReadOnlyDictionary
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(allSettings);
    }

    /// <summary>
    /// TEST 9: Verify EagerSingleton also maintains singleton behavior.
    ///
    /// This tests the alternative eager initialization implementation.
    /// </summary>
    [Fact]
    public void EagerSingleton_AlwaysReturnsSameInstance()
    {
        // Arrange & Act
        var instance1 = EagerSingleton.Instance;
        var instance2 = EagerSingleton.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// TEST 10: Verify that concurrent modifications don't cause data corruption.
    ///
    /// This test ensures that when multiple threads modify the singleton's state
    /// simultaneously, no data is lost or corrupted. All modifications should be
    /// successfully applied.
    ///
    /// NOTE: This test verifies that all updates succeed, but does NOT test
    /// the order of updates (which is non-deterministic in concurrent scenarios).
    /// </summary>
    [Fact]
    public void Instance_WhenModifiedConcurrently_AllModificationsSucceed()
    {
        // Arrange
        const int threadCount = 10;
        var config = ConfigurationManager.Instance;

        // Act
        var threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            threads[i] = new Thread(() =>
            {
                // Each thread sets a unique key-value pair
                config.SetSetting($"ConcurrentKey{threadId}", $"Value{threadId}");
            });
        }

        // Start all threads
        foreach (var thread in threads)
        {
            thread.Start();
        }

        // Wait for all threads to complete
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        // All modifications from all threads should be present
        for (int i = 0; i < threadCount; i++)
        {
            var value = config.GetSetting($"ConcurrentKey{i}");
            Assert.Equal($"Value{i}", value);
        }
    }
}

/// <summary>
/// Additional test class to demonstrate that the singleton instance
/// is shared even across different test classes in different test runs.
///
/// Note: xUnit creates a new instance of the test class for each test,
/// but the Singleton instance itself remains the same across all tests.
/// </summary>
public class SingletonCrossClassTests
{
    /// <summary>
    /// Verify that the singleton instance is the same across different test classes.
    ///
    /// This demonstrates that the singleton truly has application-wide scope.
    /// </summary>
    [Fact]
    public void Instance_AccessedFromDifferentClass_ReturnsSameInstance()
    {
        // Arrange & Act
        // Access from this test class
        var instanceFromThisClass = ConfigurationManager.Instance;

        // Simulate access from another part of the application
        // (in real code, this would be from a completely different class/module)
        var instanceFromAnotherContext = GetSingletonFromAnotherMethod();

        // Assert
        Assert.Same(instanceFromThisClass, instanceFromAnotherContext);
    }

    /// <summary>
    /// Helper method to simulate accessing the singleton from another context.
    /// In a real application, this would be a method in a different class/module.
    /// </summary>
    private ConfigurationManager GetSingletonFromAnotherMethod()
    {
        return ConfigurationManager.Instance;
    }
}
