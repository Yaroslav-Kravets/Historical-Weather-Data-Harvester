// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Common;

using System.Runtime.CompilerServices;

public static class Argument
{
    public static void ThrowIfNull(
        object? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void ThrowIfNullOrEmpty<T>(
        IEnumerable<T>? collection,
        string message = "Collection must not be empty.",
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection);

        if (IsEmpty(collection!))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void ThrowIf<T>(
        T value,
        Func<T, bool> failurePredicate,
        string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ThrowIfNull(failurePredicate);

        if (failurePredicate(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    private static bool IsEmpty<T>(IEnumerable<T> collection)
    {
        if (collection is ICollection<T> genericCollection)
        {
            return genericCollection.Count == 0;
        }

        if (collection is IReadOnlyCollection<T> readOnlyCollection)
        {
            return readOnlyCollection.Count == 0;
        }

        return !collection.Any();
    }
}
