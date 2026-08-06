namespace UpcsgWeb.Shared.Contracts;

/// <summary>
/// The short, human-sized name for an order: <c>K7F3-QM2X</c> rather than
/// <c>0198c4e2-6b1f-7a3d-9c44-1f2e3a4b5c6d</c>.
///
/// Officers and guilders have to say these out loud at a merch table, type them into a
/// group chat, and write them on a claim slip. A GUID cannot survive any of that.
///
/// DISPLAY ONLY. Every route, lookup and foreign key stays on the GUID — see the note on
/// collisions below, which is only tolerable because nothing resolves an order *from* this
/// string. Do not add a lookup-by-reference endpoint without first making the reference a
/// stored, uniqueness-enforced column.
/// </summary>
public static class OrderReference
{
    /// <summary>
    /// Crockford's base32: no I, L, O or U. The first three are dropped because they are
    /// indistinguishable from 1 and 0 in most fonts and every handwriting, and U because
    /// removing it keeps accidental profanity out of a code shown to students.
    /// </summary>
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Characters of base32, before the separator is inserted.</summary>
    private const int Length = 8;

    /// <summary>
    /// Derived, not stored, so it needs no column, no migration and no backfill, and it is
    /// identical everywhere it is computed.
    ///
    /// Hashed rather than sliced out of the GUID. Ids are <c>Guid.CreateVersion7</c>, whose
    /// leading bytes are a millisecond timestamp — taking a prefix would give every order
    /// placed the same afternoon a nearly identical reference, which is the opposite of
    /// what this is for. A hash spreads them across the whole space.
    ///
    /// Uniqueness is probabilistic: 8 base32 characters is 40 bits, so a collision becomes
    /// likely somewhere past a million orders. At guild scale the chance is negligible, and
    /// because this is display-only a collision would show two orders the same label rather
    /// than confusing one for the other anywhere it matters.
    /// </summary>
    public static string For(Guid id)
    {
        // FNV-1a over the canonical "N" form: culture-independent, endianness-independent,
        // and stable across processes — unlike string.GetHashCode, which is randomised per
        // process and would give the same order a different reference on every restart.
        var hash = Fnv1a(id.ToString("n"));

        Span<char> buffer = stackalloc char[Length + 1];

        for (var i = 0; i < Length; i++)
        {
            // Most significant group first, so the reference reads left to right.
            var shift = (Length - 1 - i) * 5;
            var index = (int)((hash >> shift) & 0x1F);

            // The separator makes an eight-character code readable at a glance and easier
            // to repeat over a noisy merch table.
            buffer[i < 4 ? i : i + 1] = Alphabet[index];
        }

        buffer[4] = '-';

        return new string(buffer);
    }

    /// <summary>Convenience for the many call sites that render "#REF".</summary>
    public static string Display(Guid id) => "#" + For(id);

    /// <summary>
    /// FNV-1a followed by a bit-mixing finalizer.
    ///
    /// The finalizer is not optional. FNV-1a alone avalanches poorly for short inputs that
    /// share a long prefix — which is every pair of v7 ids from the same day — and the
    /// bits this reads for the leading character barely moved: 5,000 clustered ids
    /// produced just two distinct first characters. The mixing step below is MurmurHash3's
    /// fmix64, which makes every output bit depend on every input bit.
    ///
    /// OrderReferenceTests.Spreads_ids_that_share_a_timestamp_prefix is what caught that
    /// and is what will catch it again if this is ever simplified back.
    /// </summary>
    private static ulong Fnv1a(string value)
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;

        var hash = offset;

        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        hash ^= hash >> 33;
        hash *= 0xFF51AFD7ED558CCDUL;
        hash ^= hash >> 33;
        hash *= 0xC4CEB9FE1A85EC53UL;
        hash ^= hash >> 33;

        return hash;
    }
}
