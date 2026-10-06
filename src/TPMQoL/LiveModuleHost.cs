using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TPMQoL;

public interface ITPMQoLLiveModule
{
    void Start(Harmony harmony, ManualLogSource log);
    void Tick();
    void Stop();
}

internal static class LiveModuleHost
{
    private const string HarmonyId = "ch.gutzufuss.tpmqol.live";

    private sealed class LiveLoadContext : AssemblyLoadContext
    {
        internal LiveLoadContext() : base("TPMQoLLive", isCollectible: true) { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(
                assembly => AssemblyName.ReferenceMatchesDefinition(
                    assembly.GetName(),
                    assemblyName));
        }
    }

    private static readonly string LiveDirectory =
        Path.Combine(Paths.PluginPath, "TPMQoL", "live");

    private static readonly string LiveModulePath =
        Path.Combine(LiveDirectory, "TPMQoLLive.dll");

    private static DateTime _lastWriteUtc = DateTime.MinValue;
    private static long _lastLength = -1;
    private static LiveLoadContext _loadContext;
    private static ITPMQoLLiveModule _module;
    private static Harmony _harmony;
    private static bool _reportedMissing;

    internal static void EnsureApplied()
    {
        try
        {
            if (!File.Exists(LiveModulePath))
            {
                if (_module != null)
                    UnloadCurrent("live module removed");

                if (!_reportedMissing)
                {
                    _reportedMissing = true;
                    Plugin.Log.LogInfo(
                        $"Live module host ready. Drop TPMQoLLive.dll into '{LiveDirectory}' for no-restart development reloads.");
                }
                return;
            }

            _reportedMissing = false;
            var info = new FileInfo(LiveModulePath);
            var writeUtc = info.LastWriteTimeUtc;
            var length = info.Length;

            if (_module == null || writeUtc != _lastWriteUtc || length != _lastLength)
                Reload(writeUtc, length);

            _module?.Tick();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Live module host failed: {ex}");
        }
    }

    private static void Reload(DateTime writeUtc, long length)
    {
        UnloadCurrent("reload");

        byte[] bytes;
        using (var stream = new FileStream(
                   LiveModulePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            bytes = new byte[stream.Length];
            var read = 0;
            while (read < bytes.Length)
            {
                var n = stream.Read(bytes, read, bytes.Length - read);
                if (n <= 0)
                    throw new EndOfStreamException("Could not read complete live module DLL.");
                read += n;
            }
        }

        var context = new LiveLoadContext();
        Assembly assembly;
        using (var memory = new MemoryStream(bytes, writable: false))
            assembly = context.LoadFromStream(memory);

        var moduleType = assembly.GetTypes().FirstOrDefault(type =>
            !type.IsAbstract &&
            typeof(ITPMQoLLiveModule).IsAssignableFrom(type));

        if (moduleType == null)
        {
            context.Unload();
            throw new InvalidOperationException(
                "TPMQoLLive.dll does not contain an ITPMQoLLiveModule implementation.");
        }

        var module = (ITPMQoLLiveModule)Activator.CreateInstance(moduleType);
        var harmony = new Harmony(HarmonyId);

        module.Start(harmony, Plugin.Log);

        _loadContext = context;
        _module = module;
        _harmony = harmony;
        _lastWriteUtc = writeUtc;
        _lastLength = length;

        Plugin.Log.LogInfo(
            $"Live module loaded: {assembly.GetName().Name} {assembly.GetName().Version}; " +
            $"type={moduleType.FullName}");
    }

    private static void UnloadCurrent(string reason)
    {
        if (_module == null && _loadContext == null && _harmony == null)
            return;

        try
        {
            _module?.Stop();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Live module Stop failed: {ex.Message}");
        }

        try
        {
            _harmony?.UnpatchSelf();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Live module unpatch failed: {ex.Message}");
        }

        _module = null;
        _harmony = null;

        var oldContext = _loadContext;
        _loadContext = null;
        oldContext?.Unload();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Plugin.Log.LogInfo($"Live module unloaded ({reason}).");
    }
}
