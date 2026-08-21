using ValueObjectPattern.Domain;
using ValueObjectPattern.Values;

namespace ValueObjectPattern.Tests;

public sealed class MoneyTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Money_EqualWhen_SameAmountAndCurrency()
    {
        var a = new Money(500m, "CAD");
        var b = new Money(500m, "CAD");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Money_NotEqual_WhenAmountDiffers()
    {
        Assert.NotEqual(new Money(100m, "CAD"), new Money(200m, "CAD"));
    }

    [Fact]
    public void Money_NotEqual_WhenCurrencyDiffers()
    {
        Assert.NotEqual(new Money(100m, "CAD"), new Money(100m, "USD"));
    }

    [Fact]
    public void Money_Currency_NormalizedToUpperCase()
    {
        var m = new Money(100m, "cad");

        Assert.Equal("CAD", m.Currency);
        Assert.Equal(new Money(100m, "CAD"), m);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Money_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1m, "CAD"));
    }

    [Fact]
    public void Money_EmptyCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(100m, ""));
    }

    [Fact]
    public void Money_Zero_Helper_ReturnsZeroAmount()
    {
        var z = Money.Zero("CAD");

        Assert.Equal(0m, z.Amount);
        Assert.Equal("CAD", z.Currency);
    }

    // ── Arithmetic ────────────────────────────────────────────────────────────

    [Fact]
    public void Money_Add_ReturnsSumInSameCurrency()
    {
        var result = new Money(300m, "CAD") + new Money(200m, "CAD");

        Assert.Equal(new Money(500m, "CAD"), result);
    }

    [Fact]
    public void Money_Add_DifferentCurrencies_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Money(100m, "CAD") + new Money(100m, "USD"));
    }

    [Fact]
    public void Money_Subtract_ReturnsDifference()
    {
        var result = new Money(500m, "CAD") - new Money(200m, "CAD");

        Assert.Equal(new Money(300m, "CAD"), result);
    }

    [Fact]
    public void Money_Subtract_ResultNegative_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Money(100m, "CAD") - new Money(200m, "CAD"));
    }

    [Fact]
    public void Money_Multiply_RoundsToTwoDecimals()
    {
        var result = new Money(1000m, "CAD") * 0.13m;

        Assert.Equal(new Money(130.00m, "CAD"), result);
    }

    [Fact]
    public void Money_IsImmutable_OriginalUnchangedAfterArithmetic()
    {
        var original = new Money(875_000m, "CAD");
        _ = original + new Money(50_000m, "CAD");

        Assert.Equal(new Money(875_000m, "CAD"), original);
    }
}

public sealed class AddressTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Address_EqualWhen_AllFieldsMatch()
    {
        var a = new Address("100 Queen St W", "Toronto", "ON", "M5H2N2");
        var b = new Address("100 Queen St W", "Toronto", "ON", "M5H2N2");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Address_NotEqual_WhenStreetDiffers()
    {
        var a = new Address("100 Queen St W", "Toronto", "ON", "M5H2N2");
        var b = new Address("200 Queen St W", "Toronto", "ON", "M5H2N2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Address_NotEqual_WhenCityDiffers()
    {
        Assert.NotEqual(
            new Address("100 Main St", "Toronto",  "ON", "M5H2N2"),
            new Address("100 Main St", "Hamilton", "ON", "L8P4Y6"));
    }

    // ── Postal code normalization ──────────────────────────────────────────────

    [Fact]
    public void Address_PostalCode_NormalizedFromNoSpaceLowercase()
    {
        var a = new Address("100 Queen St W", "Toronto", "ON", "m5h2n2");

        Assert.Equal("M5H 2N2", a.PostalCode);
    }

    [Fact]
    public void Address_PostalCode_EqualAfterNormalization()
    {
        var a = new Address("100 Queen St W", "Toronto", "ON", "M5H2N2");
        var b = new Address("100 Queen St W", "Toronto", "ON", "m5h 2n2");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Address_InvalidPostalCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Address("100 Queen St W", "Toronto", "ON", "90210"));
    }
}

public sealed class DateRangeTests
{
    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_EqualWhen_SameStartAndEnd()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 11, 30));
        var b = new DateRange(D(2026, 9, 1), D(2026, 11, 30));

        Assert.Equal(a, b);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DateRange(D(2026, 12, 1), D(2026, 11, 1)));
    }

    [Fact]
    public void DateRange_SameDayStartAndEnd_IsValid()
    {
        var range = new DateRange(D(2026, 10, 1), D(2026, 10, 1));

        Assert.Equal(0, range.DurationDays);
    }

    // ── DurationDays ──────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_DurationDays_IsCorrect()
    {
        var range = new DateRange(D(2026, 9, 1), D(2026, 11, 30));

        Assert.Equal(90, range.DurationDays);
    }

    // ── Contains ──────────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_Contains_TrueForDateInsideRange()
    {
        var range = new DateRange(D(2026, 9, 1), D(2026, 11, 30));

        Assert.True(range.Contains(D(2026, 10, 15)));
    }

    [Fact]
    public void DateRange_Contains_TrueForBoundaryDates()
    {
        var range = new DateRange(D(2026, 9, 1), D(2026, 11, 30));

        Assert.True(range.Contains(D(2026, 9, 1)));
        Assert.True(range.Contains(D(2026, 11, 30)));
    }

    [Fact]
    public void DateRange_Contains_FalseForDateOutsideRange()
    {
        var range = new DateRange(D(2026, 9, 1), D(2026, 11, 30));

        Assert.False(range.Contains(D(2026, 12, 1)));
        Assert.False(range.Contains(D(2026, 8, 31)));
    }

    // ── Overlaps ──────────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_Overlaps_TrueWhenRangesIntersect()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 11, 30));
        var b = new DateRange(D(2026, 10, 1), D(2026, 12, 31));

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void DateRange_Overlaps_TrueWhenAdjacent()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 9, 30));
        var b = new DateRange(D(2026, 9, 30), D(2026, 10, 31));

        Assert.True(a.Overlaps(b));
    }

    [Fact]
    public void DateRange_Overlaps_FalseWhenDisjoint()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 9, 30));
        var b = new DateRange(D(2026, 10, 1), D(2026, 10, 31));

        Assert.False(a.Overlaps(b));
    }

    // ── Intersection ──────────────────────────────────────────────────────────

    [Fact]
    public void DateRange_Intersection_ReturnsOverlappingRange()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 11, 30));
        var b = new DateRange(D(2026, 10, 1), D(2026, 12, 31));

        var result = a.Intersection(b);

        Assert.Equal(new DateRange(D(2026, 10, 1), D(2026, 11, 30)), result);
    }

    [Fact]
    public void DateRange_Intersection_NullWhenDisjoint()
    {
        var a = new DateRange(D(2026, 9, 1), D(2026, 9, 30));
        var b = new DateRange(D(2026, 10, 1), D(2026, 10, 31));

        Assert.Null(a.Intersection(b));
    }
}

public sealed class PropertyListingTests
{
    private static PropertyListing MakeListing() => new(
        id:           1,
        title:        "Yaletown Loft",
        location:     new Address("1200 Richards St", "Vancouver", "BC", "V6B3G6"),
        askingPrice:  new Money(1_100_000m, "CAD"),
        availability: new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)));

    [Fact]
    public void PropertyListing_WithPrice_ReturnsNewListingWithUpdatedPrice()
    {
        var listing = MakeListing();
        var reduced = listing.WithPrice(new Money(1_050_000m, "CAD"));

        Assert.Equal(new Money(1_050_000m, "CAD"), reduced.AskingPrice);
        Assert.Equal(new Money(1_100_000m, "CAD"), listing.AskingPrice);  // original unchanged
    }

    [Fact]
    public void PropertyListing_TwoListings_SameAddressValues_AreEqualByAddress()
    {
        var a = new PropertyListing(10, "Unit A",
            new Address("1200 Richards St", "Vancouver", "BC", "V6B3G6"),
            new Money(900_000m, "CAD"),
            new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));
        var b = new PropertyListing(11, "Unit B",
            new Address("1200 Richards St", "Vancouver", "BC", "v6b3g6"),
            new Money(910_000m, "CAD"),
            new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));

        Assert.Equal(a.Location, b.Location);
        Assert.Equal(a.Availability, b.Availability);
    }
}
