namespace task2.Services.Implementations;

internal static class MoneyAllocator
{
    public static decimal[] Allocate(decimal total, IReadOnlyList<decimal> weights)
    {
        var totalCents = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        var weightSum = weights.Sum();

        var cents = new long[weights.Count];
        var remainders = new decimal[weights.Count];
        long assigned = 0;

        for (var i = 0; i < weights.Count; i++)
        {
            var exact = totalCents * weights[i] / weightSum;
            cents[i] = (long)Math.Floor(exact);
            remainders[i] = exact - cents[i];
            assigned += cents[i];
        }

        var left = (int)(totalCents - assigned);
        var order = Enumerable.Range(0, weights.Count)
            .OrderByDescending(i => remainders[i])
            .ThenBy(i => i)
            .Take(left);

        foreach (var index in order)
            cents[index]++;

        return cents.Select(c => c / 100m).ToArray();
    }
}
