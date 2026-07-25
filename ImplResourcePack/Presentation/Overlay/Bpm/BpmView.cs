using System;
using System.Globalization;
using System.Text;
using ImplResourcePack.Application;
using ImplResourcePack.Domain.Bpm;
using ImplResourcePack.Presentation.Overlay.Shared;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImplResourcePack.Presentation.Overlay.Bpm;

internal sealed class BpmView : IOverlayView<BpmSnapshot>
{
  private readonly StringBuilder _builder = new(192);
  private GameObject _root;
  private TextMeshProUGUI _text;
  private double _lastTileBpm = double.NaN;
  private double _lastCurrentBpm = double.NaN;

  public BpmView(OverlayCanvasHost host, TMP_FontAsset font, Material material)
  {
    _root = OverlayTextFactory.CreateRect(
      "BPM",
      host.Root.transform,
      new Vector2(1f, 1f),
      new Vector2(1f, 1f),
      new Vector2(-16f, -16f),
      new Vector2(456f, 90f)
    );
    _text = OverlayTextFactory.AddText(_root, font, material, 25f, TextAlignmentOptions.TopRight);
    _text.lineSpacing = 30f;
    _text.color = Color.white;
    _root.SetActive(false);
  }

  public void Show()
  {
    if (_root != null)
      _root.SetActive(true);
  }

  public void Hide()
  {
    if (_root != null)
      _root.SetActive(false);
  }

  public void Render(BpmSnapshot snapshot)
  {
    if (
      _text == null
      || !snapshot.IsValid
      || (_lastTileBpm == snapshot.TileBpm && _lastCurrentBpm == snapshot.CurrentBpm)
    )
      return;

    string tileColor = OverlayTheme.ColorToHex(OverlayTheme.BpmColor(snapshot.TileBpm));
    string currentColor = OverlayTheme.ColorToHex(OverlayTheme.BpmColor(snapshot.CurrentBpm));

    _builder.Clear();
    AppendLine("TBPM", snapshot.TileBpm, tileColor);
    _builder.Append('\n');
    AppendLine("CBPM", snapshot.CurrentBpm, currentColor);
    _builder.Append('\n');
    AppendLine("KPS", snapshot.KeysPerSecond, currentColor);
    _text.text = _builder.ToString();

    _lastTileBpm = snapshot.TileBpm;
    _lastCurrentBpm = snapshot.CurrentBpm;
  }

  public void Dispose()
  {
    if (_root != null)
      Object.Destroy(_root);

    _root = null;
    _text = null;
  }

  private void AppendLine(string label, double value, string color)
  {
    _builder
      .Append("<color=#FFFFFF>")
      .Append(label)
      .Append(" |</color> <color=#")
      .Append(color)
      .Append('>')
      .Append(Math.Round(value, 2).ToString(CultureInfo.InvariantCulture))
      .Append("</color>");
  }
}
