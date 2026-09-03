using System;
using AdofaiIpc;
using AdofaiIpc.Core;
using ImplResourcePack.Application.KeyLimiter;
using ImplResourcePack.Domain.Input;

namespace ImplResourcePack.Infrastructure.Ipc;

internal sealed class KeyLimiterIpcFeature : IDisposable
{
  private const string Namespace = "impl-resourcepack";
  private const int SchemaVersion = 1;
  private readonly KeyLimiterService _service;
  private bool _active;

  public KeyLimiterIpcFeature(KeyLimiterService service)
  {
    _service = service;
  }

  public void Enable()
  {
    if (_active)
      return;

    AdofaiIpcNamespace ipc = AdofaiIpc.AdofaiIpc.RegisterNamespace(
      Namespace,
      new IpcNamespaceInfo
      {
        DisplayName = "ImplResourcePack",
        Version = Main.Instance.Version,
        AllowedOrigins = new[]
        {
          "tauri://localhost",
          "http://tauri.localhost",
          "https://tauri.localhost",
          "http://localhost",
          "http://127.0.0.1",
        },
      }
    );
    ipc.Register("health.get", Health);
    ipc.RegisterMainThread("key-limiter.sync", Sync);
    ipc.Register("key-limiter.status", Status);
    ipc.MarkReady();
    _active = true;
    Main.Instance.Log("[IPC] Namespace ready: " + Namespace);
  }

  public void Dispose()
  {
    _service.Clear();
    if (!_active)
      return;
    _active = false;
    AdofaiIpc.AdofaiIpc.UnregisterNamespace(Namespace);
    Main.Instance?.Log("[IPC] Unregistered namespace: " + Namespace);
  }

  private static object Health(IpcRequest request) =>
    new HealthResponseDto
    {
      Ok = true,
      Mod = "ImplResourcePack",
      ModVersion = Main.Instance?.Version ?? "unknown",
      ProtocolVersion = SchemaVersion,
      ServerVersion = 1,
    };

  private object Sync(IpcRequest request)
  {
    KeyLimiterSyncRequestDto input;
    try
    {
      input = request?.Params?.ToObject<KeyLimiterSyncRequestDto>();
    }
    catch (Exception exception)
    {
      Main.Instance?.LogWarning("[IPC] Invalid key-limiter.sync payload: " + exception.Message);
      return KeyLimiterSyncResponseDto.Invalid("invalid_request");
    }

    string validationError = Validate(input);
    if (validationError != null)
      return KeyLimiterSyncResponseDto.Invalid(validationError);

    KeyLimiterSyncResponseDto response = _service.Apply(input);
    if (response.UnsupportedKeys.Length > 0)
      Main.Instance?.LogWarning(
        "[IPC] Unsupported limiter keys: " + string.Join(", ", response.UnsupportedKeys)
      );
    return response;
  }

  private object Status(IpcRequest request)
  {
    KeyLimiterSnapshot snapshot = _service.Current;
    return new KeyLimiterStatusResponseDto
    {
      SchemaVersion = SchemaVersion,
      Enabled = snapshot.Enabled,
      Profile = new KeyLimiterProfileDto
      {
        Id = snapshot.ProfileId,
        Name = snapshot.ProfileName,
      },
      SessionId = snapshot.SessionId,
      Revision = snapshot.Revision,
      SupportedKeys = snapshot.SupportedKeys,
      UnsupportedKeys = snapshot.UnsupportedKeys,
    };
  }

  private static string Validate(KeyLimiterSyncRequestDto input)
  {
    if (input == null)
      return "invalid_request";
    if (input.SchemaVersion != SchemaVersion)
      return "unsupported_schema";
    if (!string.Equals(input.Source, "impl-dm-note", StringComparison.Ordinal))
      return "invalid_source";
    if (string.IsNullOrWhiteSpace(input.ClientVersion))
      return "invalid_client_version";
    if (string.IsNullOrWhiteSpace(input.SessionId))
      return "invalid_session";
    if (input.Revision <= 0)
      return "invalid_revision";
    if (input.Profile == null || string.IsNullOrWhiteSpace(input.Profile.Id))
      return "invalid_profile";
    if (input.Keys == null)
      return "invalid_keys";
    return null;
  }
}
