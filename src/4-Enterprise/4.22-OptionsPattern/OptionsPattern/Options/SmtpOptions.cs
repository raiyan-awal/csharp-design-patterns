using System.ComponentModel.DataAnnotations;

namespace OptionsPattern.Options;

public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string FromAddress { get; set; } = "";

    public string FromDisplayName { get; set; } = "Maple Notify";

    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}
