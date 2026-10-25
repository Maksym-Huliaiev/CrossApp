namespace Core.Reports;

public static class LinqExtensions
{
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        int count) =>
        source.OrderByDescending(keySelector).Take(count);
}