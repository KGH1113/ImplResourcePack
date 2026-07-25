using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImplResourcePack.Presentation.Overlay.Combo;

internal sealed class ComboAnimationDriver : MonoBehaviour
{
  private const double DurationMilliseconds = 500d;
  private const float RestingFontSize = 78f;
  private const float BumpFontSize = 30f;
  private const float RestingTitleY = 43.505f;

  private readonly Stopwatch _stopwatch = new();
  private RectTransform _titleTransform;
  private RectTransform _valueTransform;
  private TextMeshProUGUI _valueText;

  public void Initialize(RectTransform titleTransform, RectTransform valueTransform, TextMeshProUGUI valueText)
  {
    _titleTransform = titleTransform;
    _valueTransform = valueTransform;
    _valueText = valueText;
    ResetVisual();
  }

  public void Play()
  {
    if (_titleTransform == null || _valueTransform == null || _valueText == null)
      return;

    _stopwatch.Restart();
    enabled = true;
    ApplyAnimation(0d);
  }

  public void ResetVisual()
  {
    _stopwatch.Reset();
    enabled = false;
    if (_valueText != null)
      _valueText.fontSize = RestingFontSize;
    if (_titleTransform != null)
      _titleTransform.anchoredPosition = new Vector2(0f, RestingTitleY);
  }

  private void Update()
  {
    double progress = _stopwatch.Elapsed.TotalMilliseconds / DurationMilliseconds;
    if (progress >= 1d)
    {
      progress = 1d;
      _stopwatch.Stop();
    }

    ApplyAnimation(progress);
    if (progress >= 1d)
      enabled = false;
  }

  private void ApplyAnimation(double progress)
  {
    _valueText.fontSize = BumpFontSize * OutExpoChange(progress) + RestingFontSize;
    LayoutRebuilder.ForceRebuildLayoutImmediate(_valueTransform);
    _titleTransform.anchoredPosition = new Vector2(0f, _valueTransform.sizeDelta.y / 2f);
  }

  private static float OutExpoChange(double progress)
  {
    return (float)(progress == 1d ? 0d : Math.Pow(2d, -10d * progress));
  }
}
