namespace ImplResourcePack.Infrastructure.Game.Judgement;

internal static class HitMarginPolicy
{
  public static bool IsPerfect(HitMargin hit) =>
    hit == HitMargin.PerfectMinus || hit == HitMargin.XPerfect || hit == HitMargin.PerfectPlus;

  public static bool ContinuesCombo(HitMargin hit) => IsPerfect(hit) || hit == HitMargin.Auto;
}
