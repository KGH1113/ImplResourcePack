using System;
using HarmonyLib;
using ImplResourcePack.Application;
using ImplResourcePack.Application.Features;
using ImplResourcePack.Infrastructure.Assets;
using ImplResourcePack.Infrastructure.Game.Bpm;
using ImplResourcePack.Infrastructure.Game.Judgement;
using ImplResourcePack.Infrastructure.Game.Status;
using ImplResourcePack.Presentation.Overlay.Bpm;
using ImplResourcePack.Presentation.Overlay.Combo;
using ImplResourcePack.Presentation.Overlay.Judgement;
using ImplResourcePack.Presentation.Overlay.Shared;
using ImplResourcePack.Presentation.Overlay.Status;
using UnityEngine.SceneManagement;

namespace ImplResourcePack.Bootstrap;

internal sealed class ModRuntime : IDisposable
{
  private const string HarmonyId = "ImplResourcePack";

  private readonly Main _main;
  private FontBundleLoader _fontBundleLoader;
  private OverlayCanvasHost _canvasHost;
  private OverlayMaterialFactory _materialFactory;
  private OverlayCoordinator _coordinator;
  private StatusTimeTicker _timeTicker;
  private Harmony _harmony;
  private bool _sceneHooked;
  private bool _initialized;
  private bool _disposed;

  public ModRuntime(Main main)
  {
    _main = main;
  }

  public bool Initialize()
  {
    if (_initialized)
      return true;
    if (_disposed)
      throw new ObjectDisposedException(nameof(ModRuntime));

    try
    {
      _fontBundleLoader = new FontBundleLoader(_main);
      if (!_fontBundleLoader.Load())
        return false;

      _canvasHost = new OverlayCanvasHost();
      _materialFactory = new OverlayMaterialFactory(_fontBundleLoader.FontAsset);

      JudgementFeature judgement = new(
        new JudgementReader(),
        new JudgementView(_canvasHost, _fontBundleLoader.FontAsset, _materialFactory.Material)
      );
      StatusFeature status = new(
        new StatusReader(),
        new StatusView(_canvasHost, _fontBundleLoader.FontAsset, _materialFactory.Material)
      );
      BpmFeature bpm = new(
        new BpmReader(),
        new BpmView(_canvasHost, _fontBundleLoader.FontAsset, _materialFactory.Material)
      );
      ComboFeature combo = new(new ComboView(_canvasHost, _fontBundleLoader.FontAsset, _materialFactory.Material));
      _coordinator = new OverlayCoordinator(judgement, status, bpm, combo);

      _timeTicker = _canvasHost.Root.AddComponent<StatusTimeTicker>();
      _timeTicker.Initialize(OnTimeTick);

      SceneManager.sceneUnloaded += OnSceneUnloaded;
      _sceneHooked = true;

      _harmony = new Harmony(HarmonyId);
      _harmony.PatchAll(typeof(Main).Assembly);
      _initialized = true;

      if (IsGameplayActive())
        ShowGameplay();

      return true;
    }
    catch
    {
      Dispose();
      throw;
    }
  }

  public void ShowGameplay()
  {
    Execute(
      "Show overlay",
      () =>
      {
        if (!_initialized)
          return;
        if (scrController.coopMode)
        {
          HideCore();
          return;
        }

        _canvasHost.SetVisible(true);
        _coordinator.Show();
        _timeTicker.StartTicking();
      }
    );
  }

  public void HideGameplay()
  {
    Execute("Hide overlay", HideCore);
  }

  public void OnHit(scrMarginTracker tracker, HitMargin hit)
  {
    Execute("Update judgement and combo", () => _coordinator?.RegisterHit(tracker, hit));
  }

  public void OnTrackerReset(scrMarginTracker tracker)
  {
    Execute(
      "Refresh overlay after reset",
      () =>
      {
        _coordinator?.RefreshAfterReset(tracker);
        if (_coordinator is { IsVisible: true })
          _timeTicker?.StartTicking();
      }
    );
  }

  public void OnPlayerDied(scrMarginTracker tracker)
  {
    Execute(
      "Refresh overlay after death",
      () =>
      {
        _timeTicker?.StopTicking();
        _coordinator?.RefreshAfterReset(tracker);
      }
    );
  }

  public void OnAccuracyChanged()
  {
    Execute("Update XAccuracy", () => _coordinator?.RefreshAccuracy());
  }

  public void OnFloorChanged()
  {
    Execute("Update progress and BPM", () => _coordinator?.RefreshFloor());
  }

  public void Dispose()
  {
    if (_disposed)
      return;

    _disposed = true;
    _initialized = false;

    if (_sceneHooked)
    {
      SceneManager.sceneUnloaded -= OnSceneUnloaded;
      _sceneHooked = false;
    }

    _timeTicker?.StopTicking();
    _coordinator?.Hide();
    _canvasHost?.SetVisible(false);

    _coordinator?.Dispose();
    _coordinator = null;
    _timeTicker = null;

    _canvasHost?.Dispose();
    _canvasHost = null;

    _materialFactory?.Dispose();
    _materialFactory = null;

    _fontBundleLoader?.Dispose();
    _fontBundleLoader = null;

    _harmony?.UnpatchAll(HarmonyId);
    _harmony = null;
  }

  private void OnTimeTick()
  {
    Execute("Update status time", () => _coordinator?.RefreshTime());
  }

  private void OnSceneUnloaded(Scene scene)
  {
    HideGameplay();
  }

  private void HideCore()
  {
    _timeTicker?.StopTicking();
    _coordinator?.Hide();
    _canvasHost?.SetVisible(false);
  }

  private void Execute(string context, Action action)
  {
    if (_disposed)
      return;

    try
    {
      action();
    }
    catch (Exception exception)
    {
      _main.LogException(context, exception);
    }
  }

  private static bool IsGameplayActive()
  {
    return ADOBase.controller != null
      && !ADOBase.controller.paused
      && ADOBase.conductor != null
      && ADOBase.conductor.isGameWorld;
  }
}
