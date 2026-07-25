using ImplResourcePack.Domain.Judgement;
using ImplResourcePack.Infrastructure.Game.Judgement;

namespace ImplResourcePack.Application.Features;

internal sealed class JudgementFeature : IOverlayFeature
{
  private readonly JudgementReader _reader;
  private readonly IOverlayView<JudgementSnapshot> _view;

  public JudgementFeature(JudgementReader reader, IOverlayView<JudgementSnapshot> view)
  {
    _reader = reader;
    _view = view;
  }

  public void Show()
  {
    _view.Show();
    Refresh();
  }

  public void Hide()
  {
    _view.Hide();
  }

  public void Refresh()
  {
    if (_reader.TryRead(out JudgementSnapshot snapshot))
      _view.Render(snapshot);
  }

  public void Refresh(scrMarginTracker tracker)
  {
    if (_reader.TryRead(tracker, out JudgementSnapshot snapshot))
      _view.Render(snapshot);
  }

  public void Dispose()
  {
    _view.Dispose();
  }
}
