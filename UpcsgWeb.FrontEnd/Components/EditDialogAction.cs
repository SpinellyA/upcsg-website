namespace UpcsgWeb.FrontEnd.Components;

/// <summary>
/// What an officer chose to do in an edit dialog.
///
/// Rows are click-to-open, so a table row no longer carries a delete button. Deleting
/// happens inside the editor instead, where the officer can see the whole record they are
/// about to remove rather than a single line of it.
/// </summary>
public enum EditDialogAction
{
    Save = 0,
    Delete = 1,
}
