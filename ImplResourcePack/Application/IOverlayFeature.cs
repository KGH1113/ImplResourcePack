using System;

namespace ImplResourcePack.Application;

internal interface IOverlayFeature : IDisposable
{
  void Show();

  void Hide();
}
