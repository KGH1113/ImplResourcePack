using System;
using UnityEngine;

namespace ImplResourcePack.Infrastructure.Game.Status;

internal sealed class StatusTimeTicker : MonoBehaviour
{
  private const float IntervalSeconds = 0.25f;

  private Action _tick;
  private bool _running;

  public void Initialize(Action tick)
  {
    _tick = tick;
  }

  public void StartTicking()
  {
    if (_running || _tick == null)
      return;

    _running = true;
    InvokeRepeating(nameof(Tick), 0f, IntervalSeconds);
  }

  public void StopTicking()
  {
    if (!_running)
      return;

    CancelInvoke(nameof(Tick));
    _running = false;
  }

  private void Tick()
  {
    _tick?.Invoke();
  }

  private void OnDisable()
  {
    StopTicking();
  }

  private void OnDestroy()
  {
    StopTicking();
    _tick = null;
  }
}
