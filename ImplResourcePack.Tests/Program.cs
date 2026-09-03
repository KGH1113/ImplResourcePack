using System;
using ImplResourcePack.Application.KeyLimiter;
using ImplResourcePack.Domain.Judgement;
using ImplResourcePack.Infrastructure.Game.Input;
using ImplResourcePack.Infrastructure.Ipc;
using UnityEngine;

namespace ImplResourcePack.Tests;

internal static class Program
{
  private static int _passed;

  public static int Main()
  {
    try
    {
      Run("normalizes DM Note names", NormalizeNames);
      Run("maps representative keyboard, mouse, and gamepad keys", MapsRepresentativeKeys);
      Run("maps macOS modifier async keys", MapsMacModifierAsyncKeys);
      Run("applies supported keys and reports unsupported keys", AppliesSupportedSubset);
      Run("handles duplicate and stale revisions", HandlesRevisionOrdering);
      Run("disables when no requested key is supported", DisablesUnsupportedOnlyProfile);
      Run("applies hit text visibility settings", AppliesHitTextVisibilitySettings);
      Console.WriteLine("ImplResourcePack tests: " + _passed + " passed");
      return 0;
    }
    catch (Exception exception)
    {
      Console.Error.WriteLine(exception);
      return 1;
    }
  }

  private static void NormalizeNames()
  {
    Equal("LEFTSHIFT", DmNoteKeyMapper.Normalize(" left shift "));
    Equal("GPA", DmNoteKeyMapper.Normalize("GP_A"));
  }

  private static void MapsRepresentativeKeys()
  {
    MapsTo("A", KeyCode.A);
    MapsTo("LEFT SHIFT", KeyCode.LeftShift);
    MapsTo("25", KeyCode.RightControl);
    MapsTo("FORWARD SLASH", KeyCode.Slash);
    MapsTo("NUMPAD RETURN", KeyCode.KeypadEnter);
    MapsTo("MOUSE5", KeyCode.Mouse4);
    MapsTo("GP_A", KeyCode.JoystickButton0);
    MapsTo("GP_RIGHT", KeyCode.JoystickButton15);
  }

  private static void MapsMacModifierAsyncKeys()
  {
    MacModifierMapsTo(KeyCode.LeftControl, 0xE0);
    MacModifierMapsTo(KeyCode.LeftShift, 0xE1);
    MacModifierMapsTo(KeyCode.LeftAlt, 0xE2);
    MacModifierMapsTo(KeyCode.LeftMeta, 0xE3);
    MacModifierMapsTo(KeyCode.RightControl, 0xE4);
    MacModifierMapsTo(KeyCode.RightShift, 0xE5);
    MacModifierMapsTo(KeyCode.RightAlt, 0xE6);
    MacModifierMapsTo(KeyCode.RightMeta, 0xE7);
    True(
      !DmNoteKeyMapper.TryGetMacModifierAsyncKey(KeyCode.A, out _),
      "ordinary keys must not use the modifier fallback"
    );
    MapsAsyncTo("LEFT SHIFT", KeyCode.LeftShift, 0xE1);
    MapsAsyncTo("RIGHT SHIFT", KeyCode.RightShift, 0xE5);
  }

  private static void AppliesSupportedSubset()
  {
    KeyLimiterService service = new();
    KeyLimiterSyncResponseDto response = service.Apply(Request(1, true, "A", "FN", "DOT"));
    True(response.Ok && response.Applied && response.Enabled, "request should be applied");
    Sequence(new[] { "A", "DOT" }, response.SupportedKeys);
    Sequence(new[] { "FN" }, response.UnsupportedKeys);
    True(service.Current.Allows(KeyCode.A), "A should be allowed");
    True(!service.Current.Allows(KeyCode.B), "B should be blocked");
    True(service.Current.Allows(KeyCode.Escape), "Escape should always be allowed");
  }

  private static void HandlesRevisionOrdering()
  {
    KeyLimiterService service = new();
    service.Apply(Request(2, true, "A"));
    Equal("duplicate", service.Apply(Request(2, true, "S")).Reason);
    Equal("stale_revision", service.Apply(Request(1, true, "S")).Reason);
    True(service.Current.Allows(KeyCode.A), "stale requests must not replace state");
  }

  private static void DisablesUnsupportedOnlyProfile()
  {
    KeyLimiterService service = new();
    KeyLimiterSyncResponseDto response = service.Apply(Request(1, true, "FN"));
    True(response.Applied && !response.Enabled, "unsupported-only profile must fail open");
  }

  private static void AppliesHitTextVisibilitySettings()
  {
    True(HitTextVisibilityPolicy.ShouldShow(false, false, true), "Perfect should show when its setting is disabled");
    True(!HitTextVisibilityPolicy.ShouldShow(true, false, true), "Perfect should hide when its setting is enabled");
    True(
      HitTextVisibilityPolicy.ShouldShow(true, false, false),
      "non-Perfect judgments should remain visible outside record mode"
    );
    True(!HitTextVisibilityPolicy.ShouldShow(false, true, false), "record mode should hide every judgment");
  }

  private static KeyLimiterSyncRequestDto Request(long revision, bool enabled, params string[] keys) =>
    new()
    {
      SchemaVersion = 1,
      Source = "impl-dm-note",
      ClientVersion = "0.1.0",
      SessionId = "test-session",
      Revision = revision,
      Enabled = enabled,
      Profile = new KeyLimiterProfileDto { Id = "4key", Name = "4key" },
      Keys = keys,
      ExcludedGhostKeys = Array.Empty<string>(),
    };

  private static void MapsTo(string input, KeyCode expected)
  {
    True(DmNoteKeyMapper.TryResolve(input, out ResolvedLimiterKey actual), input + " should map");
    Equal(expected, actual.UnityKey);
  }

  private static void MacModifierMapsTo(KeyCode key, ushort expected)
  {
    True(
      DmNoteKeyMapper.TryGetMacModifierAsyncKey(key, out ushort actual),
      key + " should have a macOS async fallback"
    );
    Equal(expected, actual);
  }

  private static void MapsAsyncTo(string input, KeyCode expectedKey, ushort expectedAsyncKey)
  {
    True(DmNoteKeyMapper.TryResolve(input, out ResolvedLimiterKey actual), input + " should map");
    Equal(expectedKey, actual.UnityKey);
    True(actual.HasAsyncKey, input + " should have an async key");
    Equal(expectedAsyncKey, actual.AsyncKey);
  }

  private static void Run(string name, Action test)
  {
    test();
    _passed++;
    Console.WriteLine("PASS " + name);
  }

  private static void True(bool value, string message)
  {
    if (!value)
      throw new InvalidOperationException(message);
  }

  private static void Equal<T>(T expected, T actual)
  {
    if (!Equals(expected, actual))
      throw new InvalidOperationException("Expected " + expected + ", got " + actual);
  }

  private static void Sequence(string[] expected, string[] actual)
  {
    Equal(string.Join("|", expected), string.Join("|", actual));
  }
}
