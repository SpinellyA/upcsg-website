using System.Text.Json;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.FrontEnd.Components;

/// <summary>
/// Holds the working copy of the item an officer is editing.
///
/// The point of this class is live preview: the page renders <see cref="Display"/> instead
/// of the item straight from the list, so while the drawer is open the real page — real
/// fonts, real image component, real paragraph splitting — re-renders as they type. No
/// separate preview renderer to keep in sync, because there isn't one.
///
/// Edits go to a clone, so Cancel is a genuine discard rather than an undo that has to
/// reconstruct the original.
/// </summary>
public sealed class ContentDraft<T> where T : class
{
    private string _original = string.Empty;

    /// <summary>The working copy. Null when nothing is being edited.</summary>
    public T? Value { get; private set; }

    /// <summary>Id of the item being edited; 0 means a new one.</summary>
    public int EditingId { get; private set; }

    public bool IsOpen => Value is not null;

    public bool IsSaving { get; private set; }

    /// <summary>
    /// Compares against the snapshot taken when the drawer opened, so closing without
    /// touching anything doesn't nag, and typing then undoing correctly reads as clean.
    /// </summary>
    public bool IsDirty => Value is not null && Snapshot(Value) != _original;

    public void Open(T item, int id)
    {
        Value = Clone(item);
        EditingId = id;
        _original = Snapshot(Value);
    }

    public void Close()
    {
        Value = null;
        EditingId = 0;
        _original = string.Empty;
        IsSaving = false;
    }

    /// <summary>Marks the current state as saved, so the dirty guard resets without closing.</summary>
    public void AcceptAsSaved()
    {
        if (Value is not null)
        {
            _original = Snapshot(Value);
        }
    }

    public void BeginSave() => IsSaving = true;

    public void EndSave() => IsSaving = false;

    /// <summary>
    /// The version of <paramref name="item"/> the page should render: the draft while this
    /// item is the one open in the drawer, otherwise the item itself.
    /// </summary>
    public T Display(T item, Func<T, int> idOf) =>
        IsOpen && idOf(item) == EditingId ? Value! : item;

    // A JSON round-trip clones and compares in one mechanism. These DTOs are small, flat,
    // and edited one at a time, so the cost is irrelevant next to not hand-writing a copy
    // constructor per type and forgetting a field when one is added.
    private static T Clone(T item) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item, UpcsgJson.Options), UpcsgJson.Options)!;

    private static string Snapshot(T item) => JsonSerializer.Serialize(item, UpcsgJson.Options);
}
