using HarmonyLib;

namespace ImplResourcePack.Infrastructure.Game.Lifecycle;

[HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
internal static class GamePlayPatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    if (!GCS.practiceMode)
      Main.Instance?.Runtime?.ShowGameplay();
  }
}

[HarmonyPatch(typeof(scrPressToStart), nameof(scrPressToStart.ShowText))]
internal static class PressToStartPatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    if (!GCS.practiceMode && scnGame.instance != null)
      return;

    Main.Instance?.Runtime?.ShowGameplay();
  }
}

[HarmonyPatch(typeof(scrUIController), nameof(scrUIController.WipeToBlack))]
internal static class WipeToBlackPatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    Main.Instance?.Runtime?.HideGameplay();
  }
}

[HarmonyPatch(typeof(scnEditor), "ResetScene")]
internal static class EditorResetPatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    Main.Instance?.Runtime?.HideGameplay();
  }
}

[HarmonyPatch(typeof(scrController), nameof(scrController.StartLoadingScene))]
internal static class LoadingScenePatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    Main.Instance?.Runtime?.HideGameplay();
  }
}
