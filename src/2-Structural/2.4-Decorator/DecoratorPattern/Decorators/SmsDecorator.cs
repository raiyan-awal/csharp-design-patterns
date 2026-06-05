namespace DecoratorPattern;

// Sends via the inner channel AND fires an SMS — one Send() call, two channels.
// SMS text is capped at 160 characters (standard single-SMS limit).
// The smsChannel action is injectable for testability.
public sealed class SmsDecorator : NotifierDecorator
{
    private readonly Action<string> _smsChannel;
    private const int SmsMaxLength = 160;

    public SmsDecorator(INotifier inner, Action<string>? smsChannel = null) : base(inner)
        => _smsChannel = smsChannel ?? (msg => Console.WriteLine($"[SMS]  {msg}"));

    public override void Send(string recipient, string subject, string body)
    {
        base.Send(recipient, subject, body);

        string combined = $"{subject}: {body}";
        string smsText  = combined.Length > SmsMaxLength
            ? $"{combined[..(SmsMaxLength - 3)]}..."
            : combined;

        _smsChannel($"To: {recipient} | {smsText}");
    }
}
