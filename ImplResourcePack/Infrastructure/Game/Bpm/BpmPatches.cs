using HarmonyLib;

namespace ImplResourcePack.Infrastructure.Game.Bpm;

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
internal static class PlanetMoveToNextFloorPatch
{
  [HarmonyPostfix]
  private static void Postfix()
  {
    Main.Instance?.Runtime?.OnFloorChanged();
  }
}
