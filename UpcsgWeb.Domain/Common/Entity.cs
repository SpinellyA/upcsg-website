namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Identity-based equality: two entities are the same if their ids match.
///
/// Transient entities (Id == 0, not yet persisted) fall back to reference equality.
/// Without that, a new entity isn't equal to itself, and List.Remove silently fails
/// to find it — which is exactly how removing an unsaved cart line would break.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }

    private bool IsTransient => Id == 0;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other || GetType() != other.GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Two different unsaved entities are never equal — they have no identity yet.
        return !IsTransient && !other.IsTransient && Id == other.Id;
    }

    public override int GetHashCode() =>
        IsTransient
            ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
}
