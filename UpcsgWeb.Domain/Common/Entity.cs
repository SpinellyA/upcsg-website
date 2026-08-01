namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Identity-based equality: two entities are the same if their ids match.
///
/// Ids are version-7 GUIDs assigned by the domain in Create, not by the database. An
/// entity therefore has identity the moment it exists, so a child added to an aggregate
/// can be compared, found and removed before anything is saved — which database-assigned
/// integers could not do, and which is exactly how removing an unsaved line used to
/// break. Version 7 is time-ordered, so it still indexes well as a primary key.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    /// <summary>Only EF's parameterless constructor should ever leave this unset.</summary>
    private bool IsTransient => Id == Guid.Empty;

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
