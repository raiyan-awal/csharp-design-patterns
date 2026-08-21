using System.Text.RegularExpressions;

namespace ValueObjectPattern.Values;

// Structural equality: two Address instances with the same fields are equal.
// PostalCode is normalized to the canonical Canadian format "A1A 1A1" on construction.
public sealed record Address
{
    // Canadian postal code pattern: letter-digit-letter [space] digit-letter-digit
    private static readonly Regex PostalPattern =
        new(@"^([A-Za-z]\d[A-Za-z])\s*(\d[A-Za-z]\d)$", RegexOptions.Compiled);

    public string Street { get; }
    public string City { get; }
    public string Province { get; }
    public string PostalCode { get; }

    public Address(string street, string city, string province, string postalCode)
    {
        Street     = street;
        City       = city;
        Province   = province;
        PostalCode = NormalizePostalCode(postalCode);
    }

    private static string NormalizePostalCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Postal code is required.");

        var m = PostalPattern.Match(raw.Trim());
        if (!m.Success)
            throw new ArgumentException($"'{raw}' is not a valid Canadian postal code.");

        return $"{m.Groups[1].Value.ToUpperInvariant()} {m.Groups[2].Value.ToUpperInvariant()}";
    }

    public override string ToString() => $"{Street}, {City}, {Province}  {PostalCode}";
}
