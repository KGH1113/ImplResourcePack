using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace ImplResourcePack.Infrastructure.Assets;

internal sealed class FontBundleLoader : IDisposable
{
  private const string BundleName = "implresourcepackfont";
  private const string FontAssetName = "MAPLESTORY_OTF_BOLD SDF";

  private readonly Main _main;
  private AssetBundle _bundle;
  private TMP_FontAsset _fallback;
  private bool _fallbackAdded;

  public TMP_FontAsset FontAsset { get; private set; }

  public FontBundleLoader(Main main)
  {
    _main = main;
  }

  public bool Load()
  {
    if (FontAsset != null)
      return true;

    string platformDirectory = GetPlatformDirectory();
    if (platformDirectory == null)
      return false;

    string bundlePath = Path.Combine(_main.Path, "Assets", "Font", platformDirectory, BundleName);
    if (!File.Exists(bundlePath))
    {
      _main.LogWarning("Font AssetBundle was not found: " + bundlePath);
      return false;
    }

    _bundle = AssetBundle.LoadFromFile(bundlePath);
    if (_bundle == null)
    {
      _main.LogWarning("Font AssetBundle could not be opened: " + bundlePath);
      return false;
    }

    FontAsset = _bundle.LoadAsset<TMP_FontAsset>(FontAssetName);
    if (FontAsset == null)
    {
      _main.LogWarning("TMP font asset was not found in the bundle: " + FontAssetName);
      _bundle.Unload(true);
      _bundle = null;
      return false;
    }

    _fallback = RDConstants.data.chineseFontTMPro;
    if (_fallback != null)
    {
      FontAsset.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
      if (!FontAsset.fallbackFontAssetTable.Contains(_fallback))
      {
        FontAsset.fallbackFontAssetTable.Add(_fallback);
        _fallbackAdded = true;
      }
    }

    _main.Log("Loaded overlay font from " + bundlePath);
    return true;
  }

  public void Dispose()
  {
    if (_fallbackAdded && FontAsset != null && FontAsset.fallbackFontAssetTable != null)
      FontAsset.fallbackFontAssetTable.Remove(_fallback);

    _fallbackAdded = false;
    _fallback = null;
    FontAsset = null;
    _bundle?.Unload(true);
    _bundle = null;
  }

  private string GetPlatformDirectory()
  {
    switch (UnityEngine.Application.platform)
    {
      case RuntimePlatform.WindowsPlayer:
      case RuntimePlatform.WindowsEditor:
        return "Windows";
      case RuntimePlatform.LinuxPlayer:
      case RuntimePlatform.LinuxEditor:
        return "Linux";
      case RuntimePlatform.OSXPlayer:
      case RuntimePlatform.OSXEditor:
        return "Mac";
      default:
        _main.LogWarning("Unsupported runtime platform for the font AssetBundle: " + UnityEngine.Application.platform);
        return null;
    }
  }
}
