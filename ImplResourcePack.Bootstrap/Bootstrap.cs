using System;
using System.IO;
using System.Reflection;
using UnityModManagerNet;

namespace ImplResourcePack.Bootstrap;

public static class Bootstrap
{
  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    try
    {
      string payloadPath = Path.Combine(modEntry.Path, "ImplResourcePack.dll");
      Assembly payload = Assembly.LoadFrom(payloadPath);
      Type main = payload.GetType("ImplResourcePack.Main", true);
      MethodInfo load = main.GetMethod(
        "Load",
        BindingFlags.Public | BindingFlags.Static,
        null,
        new[] { typeof(UnityModManager.ModEntry) },
        null
      ) ?? throw new MissingMethodException("ImplResourcePack.Main", "Load");
      return (bool)load.Invoke(null, new object[] { modEntry });
    }
    catch (TargetInvocationException exception) when (exception.InnerException != null)
    {
      modEntry.Logger.Error(exception.InnerException.ToString());
      return false;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }
}
