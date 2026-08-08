namespace UpcsgWeb.Shared.Contracts;

public static class OrderReference
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private const int Length = 8;

    public static string For(Guid id)
    {
        var hash = Fnv1a(id.ToString("n"));

        Span<char> buffer = stackalloc char[Length + 1];

        for (var i = 0; i < Length; i++)
        {
            var shift = (Length - 1 - i) * 5;
            var index = (int)((hash >> shift) & 0x1F);

            buffer[i < 4 ? i : i + 1] = Alphabet[index];
        }

        buffer[4] = '-';

        return new string(buffer);
    }

    public static string Display(Guid id) => "#" + For(id);

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
