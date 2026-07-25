using UnityEngine;

namespace ImplResourcePack.Presentation.Overlay.Shared;

internal static class OverlayTheme
{
  public static readonly Color Accent = new(186f / 255f, 113f / 255f, 43f / 255f, 1f);
  public static readonly Color JudgementBase = new(0.8509804f, 0.345098f, 1f);

  public const double BpmColorMaximum = 8000d;

  public static Color ProgressColor(float progress)
  {
    return Color.Lerp(Color.white, Accent, Mathf.Clamp01(progress));
  }

  public static Color XAccuracyColor(float xAccuracy)
  {
    float amount = Mathf.InverseLerp(0.98f, 1f, xAccuracy);
    return Color.Lerp(Accent, Color.white, amount);
  }

  public static Color BpmColor(double bpm)
  {
    return Color.Lerp(Color.white, Accent, Mathf.Clamp01((float)(bpm / BpmColorMaximum)));
  }

  public static string ColorToHex(Color color)
  {
    return ColorUtility.ToHtmlStringRGB(color);
  }

  public static Color ComboColor(int _)
  {
    return Accent;
  }
}
