namespace kg.ValheimEnchantmentSystem.UI;

// Polling can happen more than once per rendered frame. Track the held state instead of
// relying on a frame-based down flag, and require a neutral state after initialization/rebinding.
internal sealed class EnchantmentGamepadShortcutState
{
    private bool _waitingForRelease = true;
    private bool _mainHeld;

    internal bool Observe(bool mainHeld, bool modifierHeld, bool modifierRequired)
    {
        bool mainPressed = mainHeld && !_mainHeld;
        _mainHeld = mainHeld;

        if (_waitingForRelease)
        {
            if (!mainHeld && !modifierHeld) _waitingForRelease = false;
            return false;
        }

        return mainPressed && (!modifierRequired || modifierHeld);
    }

    internal void RequireRelease()
    {
        _mainHeld = false;
        _waitingForRelease = true;
    }
}
