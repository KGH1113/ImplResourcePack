using System;

namespace ImplResourcePack.Application;

internal interface IOverlayView<TSnapshot> : IDisposable
{
  void Show();

  void Hide();

  void Render(TSnapshot snapshot);
}
