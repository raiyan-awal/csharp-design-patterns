using Microsoft.Extensions.Options;
using OptionsPattern.Options;

namespace OptionsPattern.Services;

public sealed class NotificationService(IOptionsMonitor<SmtpOptions> smtpMonitor)
{
    public SmtpOptions GetTransactionalConfig() => smtpMonitor.Get("Transactional");
    public SmtpOptions GetMarketingConfig()     => smtpMonitor.Get("Marketing");

    public void SendOrderConfirmation(string customerEmail, string orderRef)
    {
        var smtp = smtpMonitor.Get("Transactional");
        Console.WriteLine($"  [{smtp.FromDisplayName}] {smtp.FromAddress} → {customerEmail}");
        Console.WriteLine($"  Subject : Your Maple order {orderRef} is confirmed");
    }

    public void SendCampaign(string customerEmail, string campaignName)
    {
        var smtp = smtpMonitor.Get("Marketing");
        Console.WriteLine($"  [{smtp.FromDisplayName}] {smtp.FromAddress} → {customerEmail}");
        Console.WriteLine($"  Campaign: {campaignName}");
    }

    public IDisposable? WatchForChanges(Action<SmtpOptions, string?> onChanged) =>
        smtpMonitor.OnChange(onChanged);
}
