namespace UpcsgWeb.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

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

        return !IsTransient && !other.IsTransient && Id == other.Id;
    }

    public override int GetHashCode() =>
        IsTransient
            ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
}
