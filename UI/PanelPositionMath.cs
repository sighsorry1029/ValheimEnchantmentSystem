using System;

namespace kg.ValheimEnchantmentSystem.UI;

internal static class PanelPositionMath
{
    internal static float SanitizeOffset(float value) => IsFinite(value) ? value : 0f;

    // Bounds describe the authored position, before applying the requested offset.
    // Keep the saved request separate from this temporary, viewport-dependent clamp.
    internal static float ClampOffset(float requested, float panelMin, float panelMax,
        float handleMin, float handleMax, float viewportMin, float viewportMax)
    {
        if (!AreValidBounds(panelMin, panelMax) || !AreValidBounds(handleMin, handleMax) ||
            !AreValidBounds(viewportMin, viewportMax))
        {
            return 0f;
        }

        double viewportSize = (double)viewportMax - viewportMin;
        double constrainedMin = panelMin;
        double constrainedMax = panelMax;
        if ((double)panelMax - panelMin > viewportSize)
        {
            constrainedMin = handleMin;
            constrainedMax = handleMax;
        }

        double result;
        if (constrainedMax - constrainedMin > viewportSize)
        {
            // If even the handle is too large, show its center instead of choosing
            // an edge that could leave the useful part of the handle off-screen.
            result = ((double)viewportMin + viewportMax - constrainedMin - constrainedMax) / 2d;
        }
        else
        {
            double minimum = viewportMin - constrainedMin;
            double maximum = viewportMax - constrainedMax;
            result = Math.Max(minimum, Math.Min(maximum, SanitizeOffset(requested)));
        }

        // Finite float bounds can still require a translation outside the float
        // range. Never return infinity to a RectTransform in that case.
        return (float)Math.Max(-float.MaxValue, Math.Min(float.MaxValue, result));
    }

    private static bool AreValidBounds(float minimum, float maximum) =>
        IsFinite(minimum) && IsFinite(maximum) && minimum <= maximum;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
