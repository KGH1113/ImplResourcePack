using System;
using System.Collections.Generic;
using System.Threading;
using ImplResourcePack.Domain.Input;
using ImplResourcePack.Infrastructure.Game.Input;
using ImplResourcePack.Infrastructure.Ipc;
using UnityEngine;

namespace ImplResourcePack.Application.KeyLimiter;

internal sealed class KeyLimiterService
{
  private readonly object _updateGate = new();
  private KeyLimiterSnapshot _current = KeyLimiterSnapshot.Disabled;

  public KeyLimiterSnapshot Current => Volatile.Read(ref _current);

  public KeyLimiterSyncResponseDto Apply(KeyLimiterSyncRequestDto request)
  {
    lock (_updateGate)
    {
      KeyLimiterSnapshot current = Current;
      if (string.Equals(current.SessionId, request.SessionId, StringComparison.Ordinal))
      {
        if (request.Revision == current.Revision)
          return KeyLimiterSyncResponseDto.From(current, false, "duplicate");
        if (request.Revision < current.Revision)
          return KeyLimiterSyncResponseDto.From(current, false, "stale_revision");
      }

      HashSet<KeyCode> allowedKeys = new();
      HashSet<ushort> allowedAsyncKeys = new();
      List<string> supported = new();
      List<string> unsupported = new();
      HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

      if (request.Enabled)
      {
        foreach (string rawKey in request.Keys ?? Array.Empty<string>())
        {
          string key = rawKey?.Trim() ?? string.Empty;
          if (string.IsNullOrEmpty(key) || !seen.Add(key))
            continue;
          if (!DmNoteKeyMapper.TryResolve(key, out ResolvedLimiterKey resolved))
          {
            unsupported.Add(key);
            continue;
          }

          supported.Add(key);
          allowedKeys.Add(resolved.UnityKey);
          DmNoteKeyMapper.AddAsyncKeys(resolved, allowedAsyncKeys);
        }
      }

      bool enabled = request.Enabled && supported.Count > 0;
      KeyLimiterSnapshot next = new(
        enabled,
        request.Profile.Id,
        request.Profile.Name,
        request.SessionId,
        request.Revision,
        supported.ToArray(),
        unsupported.ToArray(),
        allowedKeys,
        allowedAsyncKeys
      );
      Volatile.Write(ref _current, next);
      return KeyLimiterSyncResponseDto.From(next, true, "applied");
    }
  }

  public void Clear()
  {
    lock (_updateGate)
      Volatile.Write(ref _current, KeyLimiterSnapshot.Disabled);
  }
}
