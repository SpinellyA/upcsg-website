using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Orders;
using static UpcsgWeb.Domain.Tests.TestData;

namespace UpcsgWeb.Domain.Tests;

/// <summary>
/// Regression cover for the transient-entity equality bug: an unsaved entity used to
/// compare unequal to itself, so List.Remove couldn't find it and removals silently
/// did nothing.
/// </summary>
public class EntityEqualityTests
{
    [Fact]
    public void TransientEntityEqualsItself()
    {
        var cart = Cart.Create(UserId);
        cart.AddItem(Hoodie(), "M", 1);

        var line = cart.Lines[0];

        // A line has identity from the moment Create makes it, well before any save.
        Assert.NotEqual(Guid.Empty, line.Id);
        Assert.True(line.Equals(line));
    }

    [Fact]
    public void TwoDistinctTransientEntitiesAreNotEqual()
    {
        var cart = Cart.Create(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);
        cart.AddItem(hoodie, "L", 1);

        Assert.NotEqual(cart.Lines[0], cart.Lines[1]);
    }

    [Fact]
    public void RemovingAnUnsavedCartLineActuallyRemovesIt()
    {
        var cart = Cart.Create(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 2);

        cart.RemoveItem(hoodie.Id, "M");

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void RemovingAnUnsavedOrderLineActuallyRemovesIt()
    {
        var order = Order.Create(UserId);
        var hoodie = Hoodie();
        order.AddLine(hoodie, "M", 1);

        order.RemoveLine(hoodie.Id, "M");

        Assert.Empty(order.Lines);
    }
}
