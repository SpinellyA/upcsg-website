using System.Text.RegularExpressions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Domain.Tests;

public class OrderReferenceTests
{
    private static readonly Regex Shape = new("^[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}$");

    private static Guid ClusteredId(int n) =>
        Guid.Parse($"0198c4e2-6b1f-7000-8000-{n:x12}");

    [Fact]
    public void Reads_as_two_groups_of_four_unambiguous_characters()
    {
        for (var n = 0; n < 200; n++)
        {
            var reference = OrderReference.For(ClusteredId(n));

            Assert.Matches(Shape, reference);

            Assert.DoesNotContain('I', reference);
            Assert.DoesNotContain('L', reference);
            Assert.DoesNotContain('O', reference);
            Assert.DoesNotContain('U', reference);
        }
    }

    [Fact]
    public void Is_stable_for_the_same_id()
    {
        var id = ClusteredId(42);

        Assert.Equal(OrderReference.For(id), OrderReference.For(id));

        Assert.Equal("#" + OrderReference.For(id), OrderReference.Display(id));
    }

    [Fact]
    public void Spreads_ids_that_share_a_timestamp_prefix()
    {
        var references = Enumerable.Range(0, 5_000)
            .Select(n => OrderReference.For(ClusteredId(n)))
            .ToList();

        Assert.Equal(references.Count, references.Distinct().Count());

        for (var position = 0; position < 4; position++)
        {
            var distinct = references.Select(r => r[position]).Distinct().Count();
            Assert.True(distinct > 8, $"Position {position} only took {distinct} values.");
        }
    }

    [Fact]
    public void Distinguishes_ids_that_differ_by_one_bit()
    {
        var a = OrderReference.For(Guid.Parse("0198c4e2-6b1f-7000-8000-000000000000"));
        var b = OrderReference.For(Guid.Parse("0198c4e2-6b1f-7000-8000-000000000001"));

        Assert.NotEqual(a, b);
    }
}
