// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Common.Tests;

using Xunit;

public sealed class ArgumentTests
{
    [Fact]
    public void ThrowIfNull_DoesNotThrow_WhenValueIsNotNull()
    {
        Argument.ThrowIfNull("value");
        Argument.ThrowIfNull(42);
        Argument.ThrowIfNull(new object());
    }

    [Fact]
    public void ThrowIfNull_ThrowsArgumentNullException_WhenValueIsNull()
    {
        string? value = null;

        var exception = Assert.Throws<ArgumentNullException>(() => Argument.ThrowIfNull(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ThrowIfNull_UsesCallerArgumentExpression_ForParameterName()
    {
        object? items = null;

        var exception = Assert.Throws<ArgumentNullException>(() => Argument.ThrowIfNull(items));

        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void ThrowIf_DoesNotThrow_WhenPredicateIsFalse()
    {
        Argument.ThrowIf(5, count => count == 0, "Count must be non-zero.");
        Argument.ThrowIf("value", value => value.Length == 0, "Value must not be empty.");
    }

    [Fact]
    public void ThrowIf_ThrowsArgumentException_WhenPredicateIsTrue()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Argument.ThrowIf(0, count => count == 0, "Count must be non-zero."));

        Assert.StartsWith("Count must be non-zero.", exception.Message, StringComparison.Ordinal);
        Assert.Equal("0", exception.ParamName);
    }

    [Fact]
    public void ThrowIf_UsesCallerArgumentExpression_ForParameterName()
    {
        var count = 0;

        var exception = Assert.Throws<ArgumentException>(() =>
            Argument.ThrowIf(count, value => value == 0, "Count must be non-zero."));

        Assert.Equal("count", exception.ParamName);
    }

    [Fact]
    public void ThrowIf_ThrowsArgumentNullException_WhenFailurePredicateIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Argument.ThrowIf(0, null!, "Count must be non-zero."));

        Assert.Equal("failurePredicate", exception.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_DoesNotThrow_WhenCollectionHasItems()
    {
        Argument.ThrowIfNullOrEmpty(new[] { 1 });
    }

    [Fact]
    public void ThrowIfNullOrEmpty_ThrowsArgumentNullException_WhenCollectionIsNull()
    {
        int[]? collection = null;

        var exception = Assert.Throws<ArgumentNullException>(() => Argument.ThrowIfNullOrEmpty(collection));

        Assert.Equal("collection", exception.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_ThrowsArgumentException_WhenCollectionIsEmpty()
    {
        var collection = Array.Empty<int>();

        var exception = Assert.Throws<ArgumentException>(() => Argument.ThrowIfNullOrEmpty(collection));

        Assert.StartsWith("Collection must not be empty.", exception.Message, StringComparison.Ordinal);
        Assert.Equal("collection", exception.ParamName);
    }
}
