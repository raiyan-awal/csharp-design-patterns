using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Options;
using OptionsPattern.Services;

namespace OptionsPattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file sealed class FakeOptionsMonitor<T>(T defaultValue) : IOptionsMonitor<T>
{
    private readonly Dictionary<string, T>     _named     = [];
    private readonly List<Action<T, string?>>  _listeners = [];

    public T CurrentValue { get; private set; } = defaultValue;

    public void Set(string name, T value) => _named[name] = value;

    public T Get(string? name) =>
        name != null && _named.TryGetValue(name, out var v) ? v : CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return null;
    }

    public void TriggerChange(T newValue, string? name = null)
    {
        if (name == null) CurrentValue = newValue;
        else              _named[name]  = newValue;
        foreach (var l in _listeners) l(newValue, name);
    }
}

file static class Validate
{
    public static (bool IsValid, List<ValidationResult> Results) Options<T>(T instance) where T : class
    {
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return (isValid, results);
    }
}

// ── Suite 1: SmtpOptions validation ──────────────────────────────────────────

public sealed class SmtpOptionsValidationTests
{
    private static SmtpOptions Valid() => new()
    {
        Host            = "smtp.maple-notify.ca",
        Port            = 587,
        FromAddress     = "noreply@maple-notify.ca",
        FromDisplayName = "Maple Notify",
        TimeoutSeconds  = 30,
    };

    [Fact]
    public void ValidOptions_PassValidation()
    {
        var (isValid, _) = Validate.Options(Valid());
        Assert.True(isValid);
    }

    [Fact]
    public void EmptyHost_FailsValidation()
    {
        var opts = Valid(); opts.Host = "";
        var (isValid, results) = Validate.Options(opts);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SmtpOptions.Host)));
    }

    [Fact]
    public void PortZero_FailsValidation()
    {
        var opts = Valid(); opts.Port = 0;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void PortAboveMax_FailsValidation()
    {
        var opts = Valid(); opts.Port = 65536;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void InvalidEmailAddress_FailsValidation()
    {
        var opts = Valid(); opts.FromAddress = "not-an-email";
        var (isValid, results) = Validate.Options(opts);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SmtpOptions.FromAddress)));
    }

    [Fact]
    public void TimeoutBelowMin_FailsValidation()
    {
        var opts = Valid(); opts.TimeoutSeconds = 4;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void TimeoutAboveMax_FailsValidation()
    {
        var opts = Valid(); opts.TimeoutSeconds = 121;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }
}

// ── Suite 2: RetryOptions validation ─────────────────────────────────────────

public sealed class RetryOptionsValidationTests
{
    private static RetryOptions Valid() => new() { MaxAttempts = 3, BaseDelayMs = 500 };

    [Fact]
    public void ValidOptions_PassValidation()
    {
        var (isValid, _) = Validate.Options(Valid());
        Assert.True(isValid);
    }

    [Fact]
    public void MaxAttemptsZero_FailsValidation()
    {
        var opts = Valid(); opts.MaxAttempts = 0;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void MaxAttemptsAboveMax_FailsValidation()
    {
        var opts = Valid(); opts.MaxAttempts = 6;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void BaseDelayBelowMin_FailsValidation()
    {
        var opts = Valid(); opts.BaseDelayMs = 99;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }

    [Fact]
    public void BaseDelayAboveMax_FailsValidation()
    {
        var opts = Valid(); opts.BaseDelayMs = 10_001;
        var (isValid, _) = Validate.Options(opts);
        Assert.False(isValid);
    }
}

// ── Suite 3: EmailDispatcher reads options correctly ─────────────────────────

public sealed class EmailDispatcherTests
{
    private static EmailDispatcher Build(
        string host = "smtp.maple-notify.ca",
        int    port = 587,
        string from = "noreply@maple-notify.ca",
        int    timeout  = 30,
        int    attempts = 3,
        int    delay    = 500)
    {
        var smtp  = new OptionsWrapper<SmtpOptions>(new SmtpOptions  { Host = host, Port = port, FromAddress = from, TimeoutSeconds = timeout });
        var retry = new OptionsWrapper<RetryOptions>(new RetryOptions { MaxAttempts = attempts, BaseDelayMs = delay });
        return new EmailDispatcher(smtp, retry);
    }

    [Fact]
    public void SmtpConfig_Host_MatchesConfigured()
    {
        var dispatcher = Build(host: "relay.maple-notify.ca");
        Assert.Equal("relay.maple-notify.ca", dispatcher.SmtpConfig.Host);
    }

    [Fact]
    public void SmtpConfig_Port_MatchesConfigured()
    {
        var dispatcher = Build(port: 465);
        Assert.Equal(465, dispatcher.SmtpConfig.Port);
    }

    [Fact]
    public void SmtpConfig_Timeout_MatchesConfigured()
    {
        var dispatcher = Build(timeout: 45);
        Assert.Equal(45, dispatcher.SmtpConfig.TimeoutSeconds);
    }

    [Fact]
    public void RetryConfig_MaxAttempts_MatchesConfigured()
    {
        var dispatcher = Build(attempts: 5);
        Assert.Equal(5, dispatcher.RetryConfig.MaxAttempts);
    }

    [Fact]
    public void RetryConfig_BaseDelayMs_MatchesConfigured()
    {
        var dispatcher = Build(delay: 1_000);
        Assert.Equal(1_000, dispatcher.RetryConfig.BaseDelayMs);
    }
}

// ── Suite 4: NotificationService uses named options ───────────────────────────

public sealed class NotificationServiceTests
{
    private static NotificationService BuildService(
        SmtpOptions transactional,
        SmtpOptions marketing,
        SmtpOptions? defaultOpts = null)
    {
        var monitor = new FakeOptionsMonitor<SmtpOptions>(defaultOpts ?? new SmtpOptions());
        monitor.Set("Transactional", transactional);
        monitor.Set("Marketing",     marketing);
        return new NotificationService(monitor);
    }

    private static SmtpOptions Transactional() => new()
    {
        Host = "smtp.maple-notify.ca", Port = 587,
        FromAddress = "noreply@maple-notify.ca", FromDisplayName = "Maple Notify",
        TimeoutSeconds = 30,
    };

    private static SmtpOptions Marketing() => new()
    {
        Host = "smtp.maple-notify.ca", Port = 587,
        FromAddress = "campaigns@maple-notify.ca", FromDisplayName = "Maple Campaigns",
        TimeoutSeconds = 60,
    };

    [Fact]
    public void GetTransactionalConfig_ReturnsTransactionalFromAddress()
    {
        var service = BuildService(Transactional(), Marketing());
        Assert.Equal("noreply@maple-notify.ca", service.GetTransactionalConfig().FromAddress);
    }

    [Fact]
    public void GetMarketingConfig_ReturnsMarketingFromAddress()
    {
        var service = BuildService(Transactional(), Marketing());
        Assert.Equal("campaigns@maple-notify.ca", service.GetMarketingConfig().FromAddress);
    }

    [Fact]
    public void TransactionalAndMarketing_HaveDifferentFromAddresses()
    {
        var service = BuildService(Transactional(), Marketing());
        Assert.NotEqual(service.GetTransactionalConfig().FromAddress,
                        service.GetMarketingConfig().FromAddress);
    }

    [Fact]
    public void GetMarketingConfig_HasLongerTimeout_ThanTransactional()
    {
        var service = BuildService(Transactional(), Marketing());
        Assert.True(service.GetMarketingConfig().TimeoutSeconds >
                    service.GetTransactionalConfig().TimeoutSeconds);
    }
}

// ── Suite 5: FakeOptionsMonitor / IOptionsMonitor behaviour ───────────────────

public sealed class OptionsMonitorTests
{
    [Fact]
    public void Get_NamedOption_ReturnsConfiguredValue()
    {
        var monitor = new FakeOptionsMonitor<SmtpOptions>(new SmtpOptions());
        monitor.Set("Transactional", new SmtpOptions { FromAddress = "noreply@maple-notify.ca" });

        Assert.Equal("noreply@maple-notify.ca", monitor.Get("Transactional").FromAddress);
    }

    [Fact]
    public void Get_UnknownName_FallsBackToDefault()
    {
        var monitor = new FakeOptionsMonitor<SmtpOptions>(new SmtpOptions { FromAddress = "default@maple-notify.ca" });

        Assert.Equal("default@maple-notify.ca", monitor.Get("UnknownName").FromAddress);
    }

    [Fact]
    public void CurrentValue_UpdatesAfterTriggerChange()
    {
        var monitor = new FakeOptionsMonitor<SmtpOptions>(new SmtpOptions { TimeoutSeconds = 30 });

        monitor.TriggerChange(new SmtpOptions { TimeoutSeconds = 60 });

        Assert.Equal(60, monitor.CurrentValue.TimeoutSeconds);
    }

    [Fact]
    public void OnChange_CallbackFires_WhenValueChanges()
    {
        var monitor  = new FakeOptionsMonitor<SmtpOptions>(new SmtpOptions { TimeoutSeconds = 30 });
        var received = new List<SmtpOptions>();

        monitor.OnChange((opts, _) => received.Add(opts));
        monitor.TriggerChange(new SmtpOptions { TimeoutSeconds = 60 });

        Assert.Single(received);
        Assert.Equal(60, received[0].TimeoutSeconds);
    }

    [Fact]
    public void OnChange_CallbackReceivesChangedName()
    {
        var monitor       = new FakeOptionsMonitor<SmtpOptions>(new SmtpOptions());
        var receivedNames = new List<string?>();

        monitor.OnChange((_, name) => receivedNames.Add(name));
        monitor.TriggerChange(new SmtpOptions(), name: "Transactional");

        Assert.Single(receivedNames);
        Assert.Equal("Transactional", receivedNames[0]);
    }
}

// ── Suite 6: End-to-end via ServiceCollection ─────────────────────────────────

public sealed class ServiceCollectionIntegrationTests
{
    [Fact]
    public void Configure_ValidOptions_ResolveSucceeds()
    {
        var services = new ServiceCollection();
        services.Configure<SmtpOptions>(o =>
        {
            o.Host = "smtp.maple-notify.ca"; o.Port = 587;
            o.FromAddress = "noreply@maple-notify.ca"; o.TimeoutSeconds = 30;
        });
        services.Configure<RetryOptions>(o => { o.MaxAttempts = 3; o.BaseDelayMs = 500; });
        services.AddSingleton<EmailDispatcher>();

        using var provider  = services.BuildServiceProvider();
        var dispatcher      = provider.GetRequiredService<EmailDispatcher>();

        Assert.Equal("smtp.maple-notify.ca", dispatcher.SmtpConfig.Host);
        Assert.Equal(3, dispatcher.RetryConfig.MaxAttempts);
    }

    [Fact]
    public void NamedOptions_ResolvedViaMonitor_ReturnCorrectConfig()
    {
        var services = new ServiceCollection();
        services.AddOptions<SmtpOptions>("Transactional")
            .Configure(o => { o.FromAddress = "noreply@maple-notify.ca"; });
        services.AddOptions<SmtpOptions>("Marketing")
            .Configure(o => { o.FromAddress = "campaigns@maple-notify.ca"; });

        using var provider = services.BuildServiceProvider();
        var monitor        = provider.GetRequiredService<IOptionsMonitor<SmtpOptions>>();

        Assert.Equal("noreply@maple-notify.ca",  monitor.Get("Transactional").FromAddress);
        Assert.Equal("campaigns@maple-notify.ca", monitor.Get("Marketing").FromAddress);
    }

    [Fact]
    public void ValidateDataAnnotations_InvalidOptions_ThrowsOnAccess()
    {
        var services = new ServiceCollection();
        services.AddOptions<SmtpOptions>()
            .Configure(o => { o.Host = ""; o.Port = 0; o.FromAddress = "not-an-email"; })
            .ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }

    [Fact]
    public void ValidateDataAnnotations_ValidOptions_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddOptions<SmtpOptions>()
            .Configure(o =>
            {
                o.Host = "smtp.maple-notify.ca"; o.Port = 587;
                o.FromAddress = "noreply@maple-notify.ca"; o.TimeoutSeconds = 30;
            })
            .ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();

        var opts = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;
        Assert.Equal("smtp.maple-notify.ca", opts.Host);
    }
}
