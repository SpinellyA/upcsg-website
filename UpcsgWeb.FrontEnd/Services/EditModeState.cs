namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Whether an officer is currently editing the site in place.
///
/// Off by default and never persisted: an officer browsing the public site should see the
/// public site. Turning it on is a deliberate act, so a stray click can't open an editor
/// while they're showing the page to somebody.
///
/// This only drives affordances. Authorisation still lives on the API — every save goes
/// through an ExeCom-policy endpoint, so flipping this flag in a console buys nothing.
/// </summary>
public sealed class EditModeState
{
    private bool _isEditing;

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (_isEditing == value)
            {
                return;
            }

            _isEditing = value;
            Changed?.Invoke();
        }
    }

    public event Action? Changed;

    public void Toggle() => IsEditing = !IsEditing;

    public void Exit() => IsEditing = false;
}
