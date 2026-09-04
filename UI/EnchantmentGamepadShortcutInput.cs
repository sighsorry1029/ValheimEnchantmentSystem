using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

// Capture before any normal GetButton/Down/Up consumer, regardless of MonoBehaviour Update order.
// Never reset ButtonDefs: their unmodified raw held state lets LB + Y work repeatedly with LB held.
internal static class EnchantmentGamepadShortcutInput
{
    private sealed class ConsumedButton
    {
        internal readonly string Name;
        internal readonly string Path;
        internal int ReleasedFrame = -1;

        internal ConsumedButton(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    private static readonly EnchantmentGamepadShortcutState PressState = new();
    private static readonly List<ConsumedButton> Consumed = new();
    private static string _main = string.Empty;
    private static string _modifier = string.Empty;
    private static bool _polling;
    private static bool _pending;
    private static int _pendingFrame;
    private static ZInput? _input;

    internal static void Configure(string main, string modifier)
    {
        _main = main == modifier ? string.Empty : main;
        _modifier = modifier;
        _pending = false;
        PressState.RequireRelease();
        // Keep an already consumed old binding masked until its buttons have been released.
    }

    internal static bool TakePendingPress()
    {
        Poll();
        bool pressed = _pending && Time.frameCount - _pendingFrame <= 1;
        _pending = false;
        return pressed;
    }

    private static void Poll()
    {
        if (_polling) return;
        _polling = true;
        try
        {
            ZInput? input = ZInput.instance;
            if (!ReferenceEquals(input, _input))
            {
                _input = input;
                _pending = false;
                Consumed.Clear();
                PressState.RequireRelease();
            }
            if (input == null) return;

            // Drain release/history flags in BOTH loops before unmasking. A new physical press
            // ends the previous lease immediately; it is consumed again only if it forms a new chord.
            for (int i = Consumed.Count - 1; i >= 0; --i)
            {
                ConsumedButton button = Consumed[i];
                ZInput.ButtonDef? definition = input.GetButtonDef(button.Name);
                if (definition?.m_heldDynamic == true)
                {
                    if (button.ReleasedFrame >= 0) Consumed.RemoveAt(i);
                }
                else if (button.ReleasedFrame < 0) button.ReleasedFrame = Time.frameCount;
                else if (Time.frameCount > button.ReleasedFrame && !HasPendingEdges(definition)) Consumed.RemoveAt(i);
            }

            if (string.IsNullOrEmpty(_main)) return;
            ZInput.ButtonDef? main = input.GetButtonDef(_main);
            ZInput.ButtonDef? modifier = string.IsNullOrEmpty(_modifier) ? null : input.GetButtonDef(_modifier);
            // Held includes context-specific Pressed fallbacks until Tick runs. Press/Release update
            // both raw held fields together; sampling one avoids phantom edges between fixed/dynamic ticks.
            bool pressed = PressState.Observe(main?.m_heldDynamic == true, modifier?.m_heldDynamic == true,
                !string.IsNullOrEmpty(_modifier));
            // Observe first so a blocked press cannot fire later when a dialog loses focus.
            if (!pressed || !ZInput.IsGamepadEnabled() ||
                !ZInput.ShouldAcceptInputFromSource(ZInput.InputSource.Gamepad) || !VES_UI.CanHandleShortcut()) return;

            Consume(main!);
            if (modifier != null) Consume(modifier);
            _pending = true;
            _pendingFrame = Time.frameCount;
            UIGamePad.m_lastInteractFrame = Time.frameCount;
        }
        finally
        {
            _polling = false;
        }
    }

    private static bool HasPendingEdges(ZInput.ButtonDef? button)
    {
        return button != null && (button.m_wasPressedDynamic || button.m_pressedDynamic || button.m_releasedDynamic ||
                                  button.m_wasPressedFixed || button.m_pressedFixed || button.m_releasedFixed);
    }

    private static void Consume(ZInput.ButtonDef button)
    {
        string path = button.GetActionPath();
        if (string.IsNullOrEmpty(path)) return;
        foreach (ConsumedButton existing in Consumed)
            if (string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)) return;
        Consumed.Add(new ConsumedButton(button.Name, path));
    }

    private static bool IsConsumed(ZInput input, string name)
    {
        if (Consumed.Count == 0) return false;
        string? path = input.GetButtonDef(name)?.GetActionPath();
        if (path == null) return false;
        foreach (ConsumedButton button in Consumed)
            if (string.Equals(button.Path, path, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetButtonState))]
    [ClientOnlyPatch]
    private static class ZInput_TryGetButtonState_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ZInput __instance, string name, ref bool __result)
        {
            if (_polling) return true;
            Poll();
            if (!IsConsumed(__instance, name)) return true;
            __result = false;
            return false;
        }
    }
}
