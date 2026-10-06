using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TPMQoLTrainer;

internal sealed record InstallResult(
    string GameDirectory,
    bool InstalledBepInEx,
    bool InstalledOrUpdatedPlugin,
    bool CreatedConfig);

internal sealed class GameMustBeClosedException : Exception
{
    internal GameMustBeClosedException()
        : base("Two Point Museum must be closed before the mod loader or plugin can be installed or updated.")
    {
    }
}

internal static class AutoInstaller
{
    internal const string BepInExVersion = "6.0.0-be.788+5b766a3";
    private const string BepInExUrl =
        "https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip";
    private const string BepInExSha256 =
        "F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A";
    private const string PluginResource = "TPMQoL.Payload.TPMQoL.dll";

    internal static string GetConfigPath(string gameDirectory) =>
        Path.Combine(gameDirectory, "BepInEx", "config", "TPMQoL.cfg");

    internal static string? FindGameDirectory()
    {
        foreach (var candidate in CandidateGameDirectories())
        {
            if (IsValidGameDirectory(candidate))
                return Path.GetFullPath(candidate);
        }

        return null;
    }

    internal static bool IsValidGameDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return File.Exists(Path.Combine(path, "TPM.exe")) &&
               File.Exists(Path.Combine(path, "GameAssembly.dll")) &&
               Directory.Exists(Path.Combine(path, "TPM_Data"));
    }

    internal static bool IsGameRunning(string gameDirectory)
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("TPM"))
            {
                try
                {
                    var exe = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(exe) &&
                        PathsEqual(Path.GetDirectoryName(exe), gameDirectory))
                        return true;
                }
                catch
                {
                    // Ignore processes we cannot inspect.
                }
            }
        }
        catch
        {
        }

        return false;
    }

    internal static bool RequiresGameClosed(string gameDirectory)
    {
        var bepInExCore = Path.Combine(gameDirectory, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll");
        if (!File.Exists(bepInExCore))
            return true;

        var installedPlugin = Path.Combine(gameDirectory, "BepInEx", "plugins", "TPMQoL", "TPMQoL.dll");
        return !PluginMatchesEmbedded(installedPlugin);
    }

    internal static InstallResult EnsureInstalled(string gameDirectory)
    {
        if (!IsValidGameDirectory(gameDirectory))
            throw new DirectoryNotFoundException("The selected folder is not a valid Two Point Museum installation.");

        gameDirectory = Path.GetFullPath(gameDirectory);

        var installedBepInEx = false;
        var updatedPlugin = false;
        var createdConfig = false;

        var bepInExDir = Path.Combine(gameDirectory, "BepInEx");
        var coreDir = Path.Combine(bepInExDir, "core");
        var bepInExCore = Path.Combine(coreDir, "BepInEx.Unity.IL2CPP.dll");

        if (!File.Exists(bepInExCore))
        {
            if (HasDifferentBepInExCore(coreDir))
                throw new InvalidOperationException(
                    "A different BepInEx installation was detected. Remove or repair that installation before using automatic setup.");

            if (IsGameRunning(gameDirectory))
                throw new GameMustBeClosedException();

            InstallBepInEx(gameDirectory);
            installedBepInEx = true;
        }

        EnsureBepInExConfig(gameDirectory);

        var pluginPath = Path.Combine(bepInExDir, "plugins", "TPMQoL", "TPMQoL.dll");
        if (!PluginMatchesEmbedded(pluginPath))
        {
            if (IsGameRunning(gameDirectory))
                throw new GameMustBeClosedException();

            Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);
            WriteEmbeddedPlugin(pluginPath);
            updatedPlugin = true;
        }

        // Never ship or keep development hot-reload payloads as part of a public install.
        // The main plugin contains the live-module host, but TPMQoLLive.dll is developer-only.
        var liveModulePath = Path.Combine(bepInExDir, "plugins", "TPMQoL", "live", "TPMQoLLive.dll");
        if (File.Exists(liveModulePath))
        {
            try
            {
                File.Delete(liveModulePath);
            }
            catch
            {
                // A stale development module must never block the normal plugin update.
            }
        }
        var configPath = GetConfigPath(gameDirectory);
        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, DefaultModConfig, new UTF8Encoding(false));
            createdConfig = true;
        }

        return new InstallResult(gameDirectory, installedBepInEx, updatedPlugin, createdConfig);
    }

    private static IEnumerable<string> CandidateGameDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                var full = Path.GetFullPath(path);
                if (seen.Add(full))
                    candidates.Add(full);
            }
            catch
            {
            }
        }

        var baseDir = AppContext.BaseDirectory
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        Add(baseDir);
        Add(Directory.GetParent(baseDir)?.FullName);

        try
        {
            foreach (var process in Process.GetProcessesByName("TPM"))
            {
                try
                {
                    Add(Path.GetDirectoryName(process.MainModule?.FileName));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        foreach (var steamRoot in GetSteamRoots())
        {
            Add(Path.Combine(steamRoot, "steamapps", "common", "Two Point Museum"));

            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
                continue;

            foreach (var library in ReadSteamLibraries(libraryFile))
                Add(Path.Combine(library, "steamapps", "common", "Two Point Museum"));
        }

        Add(@"C:\Program Files (x86)\Steam\steamapps\common\Two Point Museum");
        Add(@"C:\Program Files\Steam\steamapps\common\Two Point Museum");

        return candidates;
    }

    private static IEnumerable<string> GetSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddRegistry(RegistryKey root, string subKey, string valueName)
        {
            try
            {
                using var key = root.OpenSubKey(subKey);
                var value = key?.GetValue(valueName) as string;
                if (!string.IsNullOrWhiteSpace(value))
                    roots.Add(value.Replace('/', '\\'));
            }
            catch
            {
            }
        }

        AddRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
        AddRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
        AddRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");

        return roots;
    }

    private static IEnumerable<string> ReadSteamLibraries(string libraryFile)
    {
        string text;

        try
        {
            text = File.ReadAllText(libraryFile);
        }
        catch
        {
            yield break;
        }

        foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.IgnoreCase))
        {
            var path = match.Groups["path"].Value.Replace(@"\\", @"\");
            if (!string.IsNullOrWhiteSpace(path))
                yield return path;
        }
    }

    private static bool HasDifferentBepInExCore(string coreDir)
    {
        if (!Directory.Exists(coreDir))
            return false;

        try
        {
            return Directory.EnumerateFiles(coreDir, "*.dll", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return true;
        }
    }

    private static void InstallBepInEx(string gameDirectory)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "TPMQoLTrainer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var zipPath = Path.Combine(tempRoot, "BepInEx.zip");

        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TPMQoLTrainer/1.0");

            using (var response = client.GetAsync(BepInExUrl, HttpCompletionOption.ResponseHeadersRead)
                       .GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();

                using var input = response.Content.ReadAsStream();
                using var output = File.Create(zipPath);
                input.CopyTo(output);
            }

            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(zipPath)));
            if (!actualHash.Equals(BepInExSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    $"Downloaded BepInEx archive failed SHA-256 verification. Expected {BepInExSha256}, got {actualHash}.");

            ZipFile.ExtractToDirectory(zipPath, gameDirectory, true);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
            catch
            {
            }
        }
    }

    private static void EnsureBepInExConfig(string gameDirectory)
    {
        var configDir = Path.Combine(gameDirectory, "BepInEx", "config");
        Directory.CreateDirectory(configDir);

        var path = Path.Combine(configDir, "BepInEx.cfg");

        if (!File.Exists(path))
        {
            File.WriteAllText(
                path,
                "[Logging]" + Environment.NewLine +
                Environment.NewLine +
                "UnityLogListening = false" + Environment.NewLine,
                new UTF8Encoding(false));
            return;
        }

        var lines = File.ReadAllLines(path).ToList();
        var sectionStart = -1;
        var sectionEnd = lines.Count;

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                if (sectionStart >= 0)
                {
                    sectionEnd = i;
                    break;
                }

                if (trimmed.Equals("[Logging]", StringComparison.OrdinalIgnoreCase))
                    sectionStart = i;
            }
        }

        if (sectionStart < 0)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add(string.Empty);

            lines.Add("[Logging]");
            lines.Add(string.Empty);
            lines.Add("UnityLogListening = false");
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            return;
        }

        for (var i = sectionStart + 1; i < sectionEnd; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("#") || !line.Contains('='))
                continue;

            var key = line[..line.IndexOf('=')].Trim();
            if (!key.Equals("UnityLogListening", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!line.EndsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = "UnityLogListening = false";
                File.WriteAllLines(path, lines, new UTF8Encoding(false));
            }

            return;
        }

        lines.Insert(sectionEnd, "UnityLogListening = false");
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static bool PluginMatchesEmbedded(string installedPath)
    {
        if (!File.Exists(installedPath))
            return false;

        try
        {
            var installed = File.ReadAllBytes(installedPath);
            var embedded = ReadEmbeddedPlugin();
            return installed.AsSpan().SequenceEqual(embedded);
        }
        catch
        {
            return false;
        }
    }

    private static void WriteEmbeddedPlugin(string destination)
    {
        var bytes = ReadEmbeddedPlugin();
        File.WriteAllBytes(destination, bytes);
    }

    private static byte[] ReadEmbeddedPlugin()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PluginResource)
            ?? throw new InvalidOperationException("Embedded TPMQoL plugin payload is missing.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        try
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd('\\', '/'),
                Path.GetFullPath(right).TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static readonly string DefaultModConfig =
        "[Applicants]" + Environment.NewLine +
        "MinimumRank = 1" + Environment.NewLine +
        "EmptySkills = true" + Environment.NewLine +
        Environment.NewLine +
        "[Diagnostics]" + Environment.NewLine +
        "LogFirstAidCandidates = false" + Environment.NewLine +
        Environment.NewLine +
        "[Expeditions]" + Environment.NewLine +
        "SurveyXPMultiplier = 1" + Environment.NewLine +
        "MaxSurveyAfterOne = false" + Environment.NewLine +
        "Quality = -1" + Environment.NewLine +
        "ForceQuality = false" + Environment.NewLine +
        Environment.NewLine +
        "[Health]" + Environment.NewLine +
        "FirstAidCuresExpeditionAilments = true" + Environment.NewLine +
        Environment.NewLine +
        "[Preservation]" + Environment.NewLine +
        "ExhibitsNeverDeteriorate = false" + Environment.NewLine +
        "ImmortalPlants = false" + Environment.NewLine +
        "AquariumsStayClean = false" + Environment.NewLine +
        Environment.NewLine +
        "[Knowledge]" + Environment.NewLine +
        "AnalysisMaxAfterOne = true" + Environment.NewLine +
        "WildlifeMaxAfterOne = true" + Environment.NewLine +
        Environment.NewLine +
        "[Security]" + Environment.NewLine +
        "CoverageRadius = 1000" + Environment.NewLine +
        Environment.NewLine +
        "[Speed]" + Environment.NewLine +
        "Training = 2" + Environment.NewLine +
        "Workshop = 2" + Environment.NewLine +
        "Analysis = 2" + Environment.NewLine +
        "Wildlife = 1" + Environment.NewLine +
        "Expeditions = 2" + Environment.NewLine +
        "StaffMovement = 1" + Environment.NewLine +
        "ExhibitExtras = 10" + Environment.NewLine;
}
