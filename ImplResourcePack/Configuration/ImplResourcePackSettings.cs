using UnityModManagerNet;

namespace ImplResourcePack.Configuration;

public sealed class ImplResourcePackSettings : UnityModManager.ModSettings
{
  public bool HidePerfectJudgmentText = true;

  public bool RecordMode;

  public override void Save(UnityModManager.ModEntry modEntry)
  {
    Save(this, modEntry);
  }
}
