using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ImplResourcePack.Presentation.Overlay.Shared;

internal sealed class OverlayCanvasHost : IDisposable
{
  public GameObject Root { get; private set; }

  public OverlayCanvasHost()
  {
    Root = new GameObject("ImplResourcePack Overlay");
    Object.DontDestroyOnLoad(Root);

    Canvas canvas = Root.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

    CanvasScaler scaler = Root.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;

    Root.SetActive(false);
  }

  public void SetVisible(bool visible)
  {
    if (Root != null)
      Root.SetActive(visible);
  }

  public void Dispose()
  {
    if (Root == null)
      return;

    Root.SetActive(false);
    Object.Destroy(Root);
    Root = null;
  }
}
