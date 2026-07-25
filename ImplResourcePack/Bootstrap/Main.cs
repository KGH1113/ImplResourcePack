using System;
using ImplResourcePack.Bootstrap;
using UnityModManagerNet;

namespace ImplResourcePack;

public sealed class Main
{
  public static Main Instance { get; private set; }

  public UnityModManager.ModEntry ModEntry { get; }
  public string Path => ModEntry.Path;
  public string Version => ModEntry.Info.Version;

  internal ModRuntime Runtime { get; private set; }

  private bool _enabled;

  private Main(UnityModManager.ModEntry modEntry)
  {
    ModEntry = modEntry;
  }

  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    try
    {
      Instance = new Main(modEntry);
      modEntry.OnToggle = OnToggle;
      modEntry.OnUnload = OnUnload;
      Instance.Enable();
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }

  public void Log(string message)
  {
    ModEntry.Logger.Log(message);
  }

  public void LogException(string context, Exception exception)
  {
    ModEntry.Logger.Error("[" + context + "] " + exception);
  }

  public void LogWarning(string message)
  {
    ModEntry.Logger.Warning(message);
  }

  private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
  {
    try
    {
      if (value)
        Instance.Enable();
      else
        Instance.Disable();
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }

  private static bool OnUnload(UnityModManager.ModEntry modEntry)
  {
    try
    {
      Instance?.Disable();
      Instance = null;
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }

  private void Enable()
  {
    if (_enabled)
      return;

    try
    {
      Runtime = new ModRuntime(this);
      if (Runtime.Initialize())
        Log("Enabled with Judgement, Status, and BPM overlays.");
      else
      {
        Runtime.Dispose();
        Runtime = null;
        LogWarning("Enabled without overlays because their font could not be loaded.");
      }

      _enabled = true;
    }
    catch
    {
      Runtime?.Dispose();
      Runtime = null;
      throw;
    }
  }

  private void Disable()
  {
    if (!_enabled)
      return;

    _enabled = false;
    Runtime?.Dispose();
    Runtime = null;
    Log("Disabled.");
  }
}
