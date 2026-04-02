using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using MAAUnified.Application.Configuration;
using MAAUnified.Application.Models;

namespace MAAUnified.Application.Services.Features;

public sealed class StageManagerFeatureService : IStageManagerFeatureService
{
    private const string DefaultClientType = "Official";
    private static readonly string[] WebRootNames = ["publish", "install"];
    private readonly UnifiedConfigurationService? _configService;
    private readonly string _baseDirectory;
    private readonly object _snapshotGate = new();
    private readonly Dictionary<string, StageSnapshot> _localSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StageSnapshot> _webSnapshots = new(StringComparer.OrdinalIgnoreCase);

    public StageManagerFeatureService()
        : this(configService: null, baseDirectory: AppContext.BaseDirectory)
    {
    }

    public StageManagerFeatureService(UnifiedConfigurationService? configService, string? baseDirectory = null)
    {
        _configService = configService;
        _baseDirectory = ResolveBaseDirectory(configService, baseDirectory);
    }

    public Task<UiOperationResult<StageManagerState>> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clientType = NormalizeClientType(ReadConfiguredClientType());
        var localSnapshot = EnsureSnapshot(clientType, preferWeb: false, forceReload: false);
        var webSnapshot = EnsureSnapshot(clientType, preferWeb: true, forceReload: false);
        return Task.FromResult(UiOperationResult<StageManagerState>.Ok(
            BuildState(clientType, localSnapshot, webSnapshot),
            "Loaded stage manager state."));
    }

    public Task<UiOperationResult<StageManagerState>> RefreshLocalAsync(
        string? clientType = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedClientType = NormalizeClientType(clientType ?? ReadConfiguredClientType());
        if (!TryLoadSnapshot(normalizedClientType, preferWeb: false, out var localSnapshot, out var errorMessage))
        {
            return Task.FromResult(UiOperationResult<StageManagerState>.Fail(
                UiErrorCode.StageManagerServiceUnavailable,
                errorMessage));
        }

        lock (_snapshotGate)
        {
            _localSnapshots[normalizedClientType] = localSnapshot!;
        }

        var webSnapshot = GetCachedSnapshot(_webSnapshots, normalizedClientType);
        return Task.FromResult(UiOperationResult<StageManagerState>.Ok(
            BuildState(normalizedClientType, localSnapshot, webSnapshot),
            $"Loaded local stage resources for `{normalizedClientType}`."));
    }

    public Task<UiOperationResult<StageManagerState>> RefreshWebAsync(
        string? clientType = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedClientType = NormalizeClientType(clientType ?? ReadConfiguredClientType());
        if (!TryLoadSnapshot(normalizedClientType, preferWeb: true, out var webSnapshot, out var errorMessage))
        {
            return Task.FromResult(UiOperationResult<StageManagerState>.Fail(
                UiErrorCode.StageManagerServiceUnavailable,
                errorMessage));
        }

        lock (_snapshotGate)
        {
            _webSnapshots[normalizedClientType] = webSnapshot!;
        }

        var localSnapshot = EnsureSnapshot(normalizedClientType, preferWeb: false, forceReload: false);
        return Task.FromResult(UiOperationResult<StageManagerState>.Ok(
            BuildState(normalizedClientType, localSnapshot, webSnapshot),
            $"Loaded web stage resources for `{normalizedClientType}`."));
    }

    public IReadOnlyList<string> GetStageCodes(string? clientType = null, bool forceReload = false)
    {
        var normalizedClientType = NormalizeClientType(clientType ?? ReadConfiguredClientType());
        var localSnapshot = EnsureSnapshot(normalizedClientType, preferWeb: false, forceReload);
        var webSnapshot = EnsureSnapshot(normalizedClientType, preferWeb: true, forceReload);
        return BuildState(normalizedClientType, localSnapshot, webSnapshot).ActiveStageCodes;
    }

    public Task<UiOperationResult<StageManagerConfig>> LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = StageManagerConfig.Default;
        if (_configService is null)
        {
            return Task.FromResult(UiOperationResult<StageManagerConfig>.Ok(config, "Loaded stage manager config."));
        }

        var current = _configService.CurrentConfig;
        config = new StageManagerConfig(
            StageCodes: ReadStageCodesFromConfig(current),
            AutoIterate: ReadBool(current, "Advanced.StageManager.AutoIterate", false),
            LastSelectedStage: ReadString(current, "Advanced.StageManager.LastSelectedStage", string.Empty),
            ClientType: NormalizeClientType(ReadString(current, "Advanced.StageManager.ClientType", DefaultClientType)));
        return Task.FromResult(UiOperationResult<StageManagerConfig>.Ok(config, "Loaded stage manager config."));
    }

    public async Task<UiOperationResult> SaveConfigAsync(StageManagerConfig config, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_configService is null)
        {
            return UiOperationResult.Fail(UiErrorCode.StageManagerServiceUnavailable, "Stage manager service is not initialized.");
        }

        foreach (var (key, value) in config.ToGlobalSettingUpdates())
        {
            _configService.CurrentConfig.GlobalValues[key] = JsonValue.Create(value);
        }

        await _configService.SaveAsync(cancellationToken);
        return UiOperationResult.Ok("Stage manager config saved.");
    }

    public Task<UiOperationResult<IReadOnlyList<string>>> ValidateStageCodesAsync(
        string stageCodesText,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var codes = (stageCodesText ?? string.Empty)
            .Split(new[] { ';', ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invalid = codes.FirstOrDefault(static code => !IsValidStageCode(code));
        if (!string.IsNullOrWhiteSpace(invalid))
        {
            return Task.FromResult(UiOperationResult<IReadOnlyList<string>>.Fail(
                UiErrorCode.StageManagerInvalidStageCode,
                $"Invalid stage code: {invalid}"));
        }

        return Task.FromResult(UiOperationResult<IReadOnlyList<string>>.Ok(codes, "Stage codes validated."));
    }

    private StageManagerState BuildState(
        string clientType,
        StageSnapshot? localSnapshot,
        StageSnapshot? webSnapshot)
    {
        var fallbackStageCodes = _configService is null
            ? Array.Empty<string>()
            : ReadStageCodesFromConfig(_configService.CurrentConfig);

        var localStageCodes = localSnapshot?.StageCodes.Count > 0 == true
            ? localSnapshot.StageCodes
            : fallbackStageCodes;
        var webStageCodes = webSnapshot?.StageCodes ?? Array.Empty<string>();

        return new StageManagerState(
            ClientType: clientType,
            LocalStageCodes: localStageCodes,
            WebStageCodes: webStageCodes,
            WebSourceUrl: webSnapshot?.SourceUrl,
            LocalRefreshedAt: localSnapshot?.RefreshedAt,
            WebRefreshedAt: webSnapshot?.RefreshedAt);
    }

    private StageSnapshot? EnsureSnapshot(string clientType, bool preferWeb, bool forceReload)
    {
        var cache = preferWeb ? _webSnapshots : _localSnapshots;
        if (!forceReload)
        {
            var cached = GetCachedSnapshot(cache, clientType);
            if (cached is not null)
            {
                return cached;
            }
        }

        if (!TryLoadSnapshot(clientType, preferWeb, out var loadedSnapshot, out _))
        {
            return GetCachedSnapshot(cache, clientType);
        }

        lock (_snapshotGate)
        {
            cache[clientType] = loadedSnapshot!;
        }

        return loadedSnapshot;
    }

    private static StageSnapshot? GetCachedSnapshot(IDictionary<string, StageSnapshot> cache, string clientType)
    {
        lock (cache)
        {
            return cache.TryGetValue(clientType, out var snapshot) ? snapshot : null;
        }
    }

    private bool TryLoadSnapshot(
        string clientType,
        bool preferWeb,
        out StageSnapshot? snapshot,
        out string errorMessage)
    {
        snapshot = null;
        errorMessage = string.Empty;

        var source = preferWeb
            ? ResolveWebSource(clientType)
            : ResolveLocalSource(clientType);
        if (source is null)
        {
            errorMessage = preferWeb
                ? $"No web stage resources found for `{clientType}` under `{_baseDirectory}`."
                : $"No local stage resources found for `{clientType}` under `{_baseDirectory}`.";
            return false;
        }

        if (!TryReadStageCodes(source, out var stageCodes, out errorMessage))
        {
            return false;
        }

        snapshot = new StageSnapshot(
            clientType,
            stageCodes,
            source.SourceUrl,
            DateTimeOffset.UtcNow);
        return true;
    }

    private StageSourceDescriptor? ResolveLocalSource(string clientType)
    {
        foreach (var baseDirectory in EnumerateBaseDirectories())
        {
            var source = TryBuildSource(Path.Combine(baseDirectory, "resource"), clientType);
            if (source is not null)
            {
                return source;
            }
        }

        return null;
    }

    private StageSourceDescriptor? ResolveWebSource(string clientType)
    {
        foreach (var baseDirectory in EnumerateBaseDirectories())
        {
            foreach (var webRootName in WebRootNames)
            {
                var source = TryBuildSource(Path.Combine(baseDirectory, webRootName, "resource"), clientType);
                if (source is not null)
                {
                    return source;
                }
            }

            var currentName = Path.GetFileName(baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (WebRootNames.Any(name => string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase)))
            {
                var source = TryBuildSource(Path.Combine(baseDirectory, "resource"), clientType);
                if (source is not null)
                {
                    return source;
                }
            }
        }

        return null;
    }

    private static StageSourceDescriptor? TryBuildSource(string resourceRoot, string clientType)
    {
        var commonStagesPath = Path.Combine(resourceRoot, "stages.json");
        var commonTasksPath = Path.Combine(resourceRoot, "tasks", "tasks.json");
        var clientFolder = NormalizeClientDirectory(clientType);
        var clientStagesPath = Path.Combine(resourceRoot, "global", clientFolder, "resource", "stages.json");
        var clientTasksPath = Path.Combine(resourceRoot, "global", clientFolder, "resource", "tasks", "tasks.json");

        var stagesPath = File.Exists(clientStagesPath)
            ? clientStagesPath
            : File.Exists(commonStagesPath)
                ? commonStagesPath
                : null;
        var tasksPath = File.Exists(clientTasksPath)
            ? clientTasksPath
            : File.Exists(commonTasksPath)
                ? commonTasksPath
                : null;

        if (stagesPath is null && tasksPath is null)
        {
            return null;
        }

        var sourceFile = stagesPath ?? tasksPath!;
        return new StageSourceDescriptor(
            stagesPath,
            tasksPath,
            new Uri(sourceFile).AbsoluteUri);
    }

    private static bool TryReadStageCodes(
        StageSourceDescriptor source,
        out IReadOnlyList<string> stageCodes,
        out string errorMessage)
    {
        var orderedCodes = new List<string>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!string.IsNullOrWhiteSpace(source.StagesPath))
            {
                var root = JsonNode.Parse(File.ReadAllText(source.StagesPath!));
                AppendStageCodesFromStagesJson(root, orderedCodes, seenCodes);
            }

            if (!string.IsNullOrWhiteSpace(source.TasksPath))
            {
                var root = JsonNode.Parse(File.ReadAllText(source.TasksPath!));
                AppendStageCodesFromTasksJson(root, orderedCodes, seenCodes);
            }
        }
        catch (Exception ex)
        {
            stageCodes = Array.Empty<string>();
            errorMessage = $"Failed to read stage resources from `{source.SourceUrl}`: {ex.Message}";
            return false;
        }

        stageCodes = orderedCodes;
        errorMessage = string.Empty;
        return orderedCodes.Count > 0;
    }

    private static void AppendStageCodesFromStagesJson(
        JsonNode? root,
        ICollection<string> target,
        ISet<string> seen)
    {
        if (root is JsonArray array)
        {
            foreach (var node in array)
            {
                if (node is not JsonObject obj || !TryReadString(obj["code"], out var code))
                {
                    continue;
                }

                AddStageCode(code, target, seen);
            }

            return;
        }

        if (root is not JsonObject objectRoot)
        {
            return;
        }

        foreach (var pair in objectRoot)
        {
            if (pair.Value is not JsonObject obj || !TryReadString(obj["code"], out var code))
            {
                continue;
            }

            AddStageCode(code, target, seen);
        }
    }

    private static void AppendStageCodesFromTasksJson(
        JsonNode? root,
        ICollection<string> target,
        ISet<string> seen)
    {
        if (root is not JsonObject objectRoot)
        {
            return;
        }

        foreach (var pair in objectRoot)
        {
            if (!ShouldTreatTaskKeyAsStageCode(pair.Key))
            {
                continue;
            }

            AddStageCode(pair.Key, target, seen);
        }
    }

    private static bool ShouldTreatTaskKeyAsStageCode(string key)
    {
        var normalized = key.Trim();
        if (string.Equals(normalized, "Annihilation", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!normalized.Contains('-', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '#')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static void AddStageCode(string code, ICollection<string> target, ISet<string> seen)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
        {
            return;
        }

        target.Add(normalized);
    }

    private static IReadOnlyList<string> ReadStageCodesFromConfig(UnifiedConfig config)
    {
        if (!config.GlobalValues.TryGetValue("Advanced.StageManager.StageCodes", out var node) || node is null)
        {
            return Array.Empty<string>();
        }

        var text = node.ToString();
        return text.Split(new[] { ';', ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string ReadConfiguredClientType()
    {
        if (_configService is null)
        {
            return DefaultClientType;
        }

        return ReadString(_configService.CurrentConfig, "Advanced.StageManager.ClientType", DefaultClientType);
    }

    private static string NormalizeClientType(string? clientType)
    {
        var normalized = string.IsNullOrWhiteSpace(clientType) ? DefaultClientType : clientType.Trim();
        return string.Equals(normalized, "Bilibili", StringComparison.OrdinalIgnoreCase)
            ? DefaultClientType
            : normalized;
    }

    private static string NormalizeClientDirectory(string clientType)
    {
        return NormalizeClientType(clientType);
    }

    private static string ResolveBaseDirectory(UnifiedConfigurationService? configService, string? baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            return Path.GetFullPath(baseDirectory);
        }

        if (configService is not null)
        {
            try
            {
                var field = typeof(UnifiedConfigurationService).GetField("_baseDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field?.GetValue(configService) is string configuredBaseDirectory && !string.IsNullOrWhiteSpace(configuredBaseDirectory))
                {
                    return Path.GetFullPath(configuredBaseDirectory);
                }
            }
            catch
            {
                // Fall back to AppContext below.
            }
        }

        return Path.GetFullPath(AppContext.BaseDirectory);
    }

    private IEnumerable<string> EnumerateBaseDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = new DirectoryInfo(_baseDirectory);
        while (current is not null)
        {
            if (seen.Add(current.FullName))
            {
                yield return current.FullName;
            }

            current = current.Parent;
        }
    }

    private static string ReadString(UnifiedConfig config, string key, string fallback)
    {
        if (config.GlobalValues.TryGetValue(key, out var node) && node is not null)
        {
            var text = node.ToString().Trim();
            if (text.Length > 0)
            {
                return text;
            }
        }

        return fallback;
    }

    private static bool ReadBool(UnifiedConfig config, string key, bool fallback)
    {
        if (config.GlobalValues.TryGetValue(key, out var node) && node is not null)
        {
            if (bool.TryParse(node.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    private static bool IsValidStageCode(string code)
    {
        foreach (var ch in code)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '#')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool TryReadString(JsonNode? node, out string value)
    {
        value = string.Empty;
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue(out string? text) || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text.Trim();
        return true;
    }

    private sealed record StageSourceDescriptor(
        string? StagesPath,
        string? TasksPath,
        string SourceUrl);

    private sealed record StageSnapshot(
        string ClientType,
        IReadOnlyList<string> StageCodes,
        string SourceUrl,
        DateTimeOffset RefreshedAt);
}
