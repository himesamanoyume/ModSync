using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT.UI;
using ModSync.UI;
using ModSync.Utility;
using SPT.Common.Utils;
using UnityEngine;

namespace ModSync;

using SyncPathFileList = Dictionary<string, List<string>>;
using SyncPathModFiles = Dictionary<string, Dictionary<string, ModFile>>;

[BepInPlugin("corter.modsync", "姫様の夢汉化 Corter ModSync", "0.10.0")]
public class Plugin : BaseUnityPlugin
{
    private static readonly string MODSYNC_DIR = Path.Combine(Directory.GetCurrentDirectory(), "ModSync_Data");
    private static readonly string PENDING_UPDATES_DIR = Path.Combine(MODSYNC_DIR, "PendingUpdates");
    private static readonly string PREVIOUS_SYNC_PATH = Path.Combine(MODSYNC_DIR, "PreviousSync.json");
    private static readonly string LOCAL_HASHES_PATH = Path.Combine(MODSYNC_DIR, "LocalHashes.json");
    private static readonly string REMOVED_FILES_PATH = Path.Combine(MODSYNC_DIR, "RemovedFiles.json");
    private static readonly string LOCAL_EXCLUSIONS_PATH = Path.Combine(MODSYNC_DIR, "Exclusions.json");
    private static readonly string UPDATER_PATH = Path.Combine(Directory.GetCurrentDirectory(), "ModSync.Updater.exe");

    private static readonly List<string> DEDICATED_DEFAULT_EXCLUSIONS =
    [
        "BepInEx/plugins/AmandsGraphics.dll",
        "BepInEx/plugins/MoreCheckmarks",
        "BepInEx/plugins/kmyuhkyuk-EFTApi",
        "BepInEx/plugins/DynamicMaps",
        "BepInEx/plugins/LootValue",
        "BepInEx/plugins/CactusPie.RamCleanerInterval.dll",
        "BepInEx/plugins/TYR_DeClutterer.dll",
    ];

    // Configuration
    private Dictionary<string, ConfigEntry<bool>> configSyncPathToggles;
    private ConfigEntry<bool> configDeleteRemovedFiles;

    private List<SyncPath> syncPaths = [];
    private SyncPathModFiles remoteModFiles = [];
    private SyncPathModFiles previousSync = [];
    private List<string> localExclusions = [];

    private SyncPathFileList addedFiles = [];
    private SyncPathFileList updatedFiles = [];
    private SyncPathFileList removedFiles = [];
    private SyncPathFileList createdDirectories = [];

    private List<Task> downloadTasks = [];

    private bool pluginFinished;
    private int downloadCount;
    private int totalDownloadCount;

    private Server server;
    private CancellationTokenSource cts = new();

    public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModSync");

    private int UpdateCount =>
        EnabledSyncPaths
            .Select(syncPath =>
                addedFiles[syncPath.path].Count
                + updatedFiles[syncPath.path].Count
                + (configDeleteRemovedFiles.Value || syncPath.enforced ? removedFiles[syncPath.path].Count : 0)
                + createdDirectories[syncPath.path].Count
            )
            .Sum();
    private static bool IsDedicated => Chainloader.PluginInfos.ContainsKey("com.fika.dedicated");
    private List<SyncPath> EnabledSyncPaths => syncPaths.Where(syncPath => configSyncPathToggles[syncPath.path].Value || syncPath.enforced).ToList();

    private bool SilentMode =>
        IsDedicated
        || EnabledSyncPaths.All(syncPath =>
            syncPath.silent
            || (
                addedFiles[syncPath.path].Count == 0
                && updatedFiles[syncPath.path].Count == 0
                && (!(configDeleteRemovedFiles.Value || syncPath.enforced) || removedFiles[syncPath.path].Count == 0)
                && createdDirectories[syncPath.path].Count == 0
            )
        );

    private bool NoRestartMode =>
        EnabledSyncPaths.All(syncPath =>
            !syncPath.restartRequired
            || (
                addedFiles[syncPath.path].Count == 0
                && updatedFiles[syncPath.path].Count == 0
                && (!(configDeleteRemovedFiles.Value || syncPath.enforced) || removedFiles[syncPath.path].Count == 0)
                && createdDirectories[syncPath.path].Count == 0
            )
        );

    private void AnalyzeModFiles(SyncPathModFiles localModFiles)
    {
        Sync.CompareModFiles(
            EnabledSyncPaths,
            localModFiles,
            remoteModFiles,
            previousSync,
            out addedFiles,
            out updatedFiles,
            out removedFiles,
            out createdDirectories
        );

        Logger.LogInfo($"发现文件 {UpdateCount} 需要下载.");
        Logger.LogInfo($"- {addedFiles.SelectMany(path => path.Value).Count()} 已添加");
        Logger.LogInfo($"- {updatedFiles.SelectMany(path => path.Value).Count()} 已更新");
        if (removedFiles.Count > 0)
            Logger.LogInfo($"- {removedFiles.SelectMany(path => path.Value).Count()} 已移除");

        if (UpdateCount > 0)
        {
            if (SilentMode)
                Task.Run(() => SyncMods(addedFiles, updatedFiles, createdDirectories));
            else
                updateWindow.Show();
        }
        else
            WriteModSyncData();
    }

    private void SkipUpdatingMods()
    {
        var enforcedAddedFiles = EnabledSyncPaths
            .ToDictionary(syncPath => syncPath.path, syncPath => syncPath.enforced ? addedFiles[syncPath.path] : [], StringComparer.OrdinalIgnoreCase);

        var enforcedUpdatedFiles = EnabledSyncPaths
            .ToDictionary(syncPath => syncPath.path, syncPath => syncPath.enforced ? updatedFiles[syncPath.path] : [], StringComparer.OrdinalIgnoreCase);

        var enforcedCreatedDirectories = EnabledSyncPaths
            .ToDictionary(syncPath => syncPath.path, syncPath => syncPath.enforced ? createdDirectories[syncPath.path] : [], StringComparer.OrdinalIgnoreCase);

        if (enforcedAddedFiles.Values.Any(files => files.Any())
            || enforcedUpdatedFiles.Values.Any(files => files.Any())
            || enforcedCreatedDirectories.Values.Any(files => files.Any()))
        {
            Task.Run(() => SyncMods(enforcedAddedFiles, enforcedUpdatedFiles, enforcedCreatedDirectories));
        }
        else
        {
            pluginFinished = true;
            updateWindow.Hide();
        }
    }

    private async Task SyncMods(SyncPathFileList filesToAdd, SyncPathFileList filesToUpdate, SyncPathFileList directoriesToCreate)
    {
        updateWindow.Hide();

        if (!Directory.Exists(PENDING_UPDATES_DIR))
            Directory.CreateDirectory(PENDING_UPDATES_DIR);

        foreach (var syncPath in EnabledSyncPaths)
        {
            foreach (var dir in directoriesToCreate[syncPath.path])
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    Logger.LogError("创建空目录失败: " + e);
                }
            }
        }

        downloadCount = 0;
        totalDownloadCount = 0;

        var limiter = new SemaphoreSlim(8);
        var filesToDownload = EnabledSyncPaths
            .Select((syncPath) => new KeyValuePair<string, List<string>>(syncPath.path, [.. filesToAdd[syncPath.path], .. filesToUpdate[syncPath.path]]))
            .ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value);

        Logger.LogInfo($"开始下载 {UpdateCount} 文件.");
        downloadTasks = EnabledSyncPaths
            .SelectMany(syncPath =>
                filesToDownload.TryGetValue(syncPath.path, out var pathFilesToDownload)
                    ? pathFilesToDownload.Select(file =>
                        server.DownloadFile(file, syncPath.restartRequired ? PENDING_UPDATES_DIR : Directory.GetCurrentDirectory(), limiter, cts.Token)
                    )
                    : []
            )
            .ToList();

        totalDownloadCount = downloadTasks.Count;

        if (!IsDedicated)
            progressWindow.Show();

        while (downloadTasks.Count > 0 && !cts.IsCancellationRequested)
        {
            var task = await Task.WhenAny(downloadTasks);

            try
            {
                await task;
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException && cts.IsCancellationRequested)
                    continue;

                cts.Cancel();
                progressWindow.Hide();
                if (!IsDedicated)
                    downloadErrorWindow.Show();
            }

            downloadTasks.Remove(task);
            downloadCount++;
        }

        downloadTasks.Clear();
        progressWindow.Hide();

        Logger.LogInfo("文件下载完成.");

        if (!cts.IsCancellationRequested)
        {
            WriteModSyncData();

            if (NoRestartMode)
            {
                Directory.Delete(PENDING_UPDATES_DIR, true);
                pluginFinished = true;
            }
            else if (!IsDedicated)
                restartWindow.Show();
            else
                StartUpdaterProcess();
        }
    }

    private async Task CancelUpdatingMods()
    {
        progressWindow.Hide();
        cts.Cancel();

        await Task.WhenAll(downloadTasks);

        Directory.Delete(PENDING_UPDATES_DIR, true);
        pluginFinished = true;
    }

    private void WriteModSyncData()
    {
        VFS.WriteTextFile(PREVIOUS_SYNC_PATH, Json.Serialize(remoteModFiles));
        if (EnabledSyncPaths.Any(syncPath => (configDeleteRemovedFiles.Value || syncPath.enforced) && removedFiles[syncPath.path].Count != 0))
            VFS.WriteTextFile(REMOVED_FILES_PATH, Json.Serialize(removedFiles.SelectMany(kvp => kvp.Value).ToList()));
    }

    private void StartUpdaterProcess()
    {
        List<string> options = [];

        if (IsDedicated)
            options.Add("--silent");

        Logger.LogInfo($"带参数启动更新器 {string.Join(" ", options)} {Process.GetCurrentProcess().Id}");
        var updaterStartInfo = new ProcessStartInfo
        {
            FileName = UPDATER_PATH,
            Arguments = string.Join(" ", options) + " " + Process.GetCurrentProcess().Id,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var updaterProcess = new Process { StartInfo = updaterStartInfo };

        updaterProcess.Start();
        Application.Quit();
    }

    private IEnumerator StartPlugin()
    {
        cts = new CancellationTokenSource();
        if (Directory.Exists(PENDING_UPDATES_DIR) || File.Exists(REMOVED_FILES_PATH))
            Logger.LogWarning(
                "ModSync找到了以前的更新. 更新器可能出错，请查看‘ModSync_Data/update.log’以了解详细信息. 将继续尝试."
            );

        Logger.LogDebug("获取服务器版本");
        var versionTask = server.GetModSyncVersion();
        yield return new WaitUntil(() => versionTask.IsCompleted);
        try
        {
            var version = versionTask.Result;

            Logger.LogInfo($"ModSync的服务器版本: {version}");
            if (version != Info.Metadata.Version.ToString())
                Logger.LogWarning($"ModSync的服务器版本与当前插件版本不匹配. 服务器版本: {version}. 插件可能不会如预期的那样工作!");
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"请求服务器版本时错误, 无法加载 {Info.Metadata.Name} . 请确保服务器mod已正确安装，然后重试."
            );
            yield break;
        }

        Logger.LogDebug("获取同步路径");
        var syncPathTask = server.GetModSyncPaths();
        yield return new WaitUntil(() => syncPathTask.IsCompleted);
        try
        {
            syncPaths = syncPathTask.Result;
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"请求同步路径时错误, 无法加载 {Info.Metadata.Name} . 请确保服务器mod已正确安装，然后重试."
            );
            yield break;
        }

        Logger.LogDebug("正在同步路径");
        foreach (var syncPath in syncPaths)
        {
            if (Path.IsPathRooted(syncPath.path))
            {
                Chainloader.DependencyErrors.Add(
                    $"由于无效的同步路径，无法加载 {Info.Metadata.Name}. 路径必须相对于 SPT 服务器根目录！无效路径 '{syncPath}'"
                );
                yield break;
            }

            if (!Path.GetFullPath(syncPath.path).StartsWith(Directory.GetCurrentDirectory()))
            {
                Chainloader.DependencyErrors.Add(
                    $"由于无效的同步路径，无法加载 {Info.Metadata.Name}。路径必须相对于 SPT 服务器根目录！无效路径 '{syncPath}'"
                );
                yield break;
            }
        }

        Logger.LogDebug("运行迁移程序");
        new Migrator(Directory.GetCurrentDirectory()).TryMigrate(Info.Metadata.Version, syncPaths);

        Logger.LogDebug("加载 syncPath 配置");
        configSyncPathToggles = syncPaths
            .Select(syncPath => new KeyValuePair<string, ConfigEntry<bool>>(
                syncPath.path,
                Config.Bind(
                    "已同步路径",
                    syncPath.path.Replace("\\", "/"),
                    syncPath.enabled,
                    new ConfigDescription(
                        $"mod应该尝试从{syncPath}同步文件吗？",
                        null,
                        new ConfigurationManagerAttributes { ReadOnly = syncPath.enforced }
                    )
                )
            ))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Logger.LogDebug("加载以前的同步数据");
        try
        {
            previousSync = VFS.Exists(PREVIOUS_SYNC_PATH) ? Json.Deserialize<SyncPathModFiles>(VFS.ReadTextFile(PREVIOUS_SYNC_PATH)) : [];
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"由于先前的同步数据不正确, 无法加载 {Info.Metadata}. 请检查ModSync_Data/PreviousSync.Json的错误或删除它, 然后再试一次."
            );
            yield break;
        }

        Logger.LogDebug("加载本地排除项");
        if (IsDedicated && !VFS.Exists(LOCAL_EXCLUSIONS_PATH))
        {
            try
            {
                VFS.WriteTextFile(LOCAL_EXCLUSIONS_PATH, Json.Serialize(DEDICATED_DEFAULT_EXCLUSIONS));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                Chainloader.DependencyErrors.Add(
                    $"由于为专用客户端编写本地排除文件出错, 无法加载{Info.Metadata}. 更多信息请查看BepInEx/LogOutput.log."
                );
                yield break;
            }
        }

        try
        {
            localExclusions = VFS.Exists(LOCAL_EXCLUSIONS_PATH) ? Json.Deserialize<List<string>>(VFS.ReadTextFile(LOCAL_EXCLUSIONS_PATH)) : [];
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"由于不正确的本地排除数据, 无法加载{Info.Metadata}. 请检查ModSync_Data/exclements.Json的错误或删除它, 然后再试一次."
            );
            yield break;
        }

        Logger.LogDebug("获取排除项");

        List<string> exclusions;
        var exclusionsTask = server.GetModSyncExclusions();
        yield return new WaitUntil(() => exclusionsTask.IsCompleted);
        try
        {
            exclusions = exclusionsTask.Result;
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"由于请求排除项错误, 无法加载{Info.Metadata}. 请确保服务器mod已正确安装, 然后重试."
            );
            yield break;
        }

        yield return new WaitUntil(() => Singleton<CommonUI>.Instantiated);

        Logger.LogDebug("本地文件哈希检测");
        var localModFilesTask = Sync.HashLocalFiles(
            Directory.GetCurrentDirectory(),
            EnabledSyncPaths,
            exclusions.Select(Glob.Create).ToList(),
            localExclusions.Select(Glob.Create).ToList()
        );

        yield return new WaitUntil(() => localModFilesTask.IsCompleted);
        var localModFiles = localModFilesTask.Result;

        VFS.WriteTextFile(LOCAL_HASHES_PATH, Json.Serialize(localModFiles));

        Logger.LogDebug("获取远端文件哈希值");
        var remoteHashesTask = server.GetRemoteModFileHashes(EnabledSyncPaths);
        yield return new WaitUntil(() => remoteHashesTask.IsCompleted);
        try
        {
            var remoteHashes = remoteHashesTask.Result;

            var localExclusionsForRemote = localExclusions.Select(Glob.CreateNoEnd).ToList();
            remoteModFiles = EnabledSyncPaths
                .Select(
                    (syncPath) =>
                    {
                        var remotePathHashes = remoteHashes[syncPath.path];

                        if (!syncPath.enforced)
                            remotePathHashes = remotePathHashes
                                .Where((kvp) => !Sync.IsExcluded(localExclusionsForRemote, kvp.Key))
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

                        return new KeyValuePair<string, Dictionary<string, ModFile>>(syncPath.path, remotePathHashes);
                    }
                )
                .ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"由于请求服务器mod列表出错, 无法加载{Info.Metadata}. 请检查服务器日志并重试."
            );
        }

        Logger.LogDebug("对比文件哈希值");
        try
        {
            AnalyzeModFiles(localModFiles);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Chainloader.DependencyErrors.Add(
                $"由于本地mods哈希值错误,无法加载 {Info.Metadata.Name}. 请确保没有文件正在运行中，然后再试一次."
            );
        }
    }

    private readonly UpdateWindow updateWindow = new("已安装的mod与服务器不匹配", "你是否想要进行更新?");
    private readonly ProgressWindow progressWindow = new("下载更新...", "你的游戏需要重启\n在更新完成之后.");
    private readonly AlertWindow restartWindow = new(new Vector2(480f, 200f), "更新完成.", "请重启游戏后继续.");
    private readonly AlertWindow downloadErrorWindow =
        new(
            new Vector2(640f, 240f),
            "下载失败!",
            "更新mod文件出错. 请查看BepInEx/LogOutput.log了解更多信息.",
            "退出"
        );

    private void Awake()
    {
        ConsoleScreen.Processor.RegisterCommand(
            "modsync",
            () =>
            {
                ConsoleScreen.Log("检查更新.");
                StartCoroutine(StartPlugin());
            }
        );

        server = new Server(Info.Metadata.Version);

        configDeleteRemovedFiles = Config.Bind("全局", "删除被移除的文件", true, "是否应该删除被服务器移除掉后已不再需要的本地文件?");
    }

    private List<string> _optional;
    private List<string> optional =>
        _optional ??= EnabledSyncPaths
            .Where(syncPath => !syncPath.enforced)
            .SelectMany(syncPath =>
                addedFiles[syncPath.path]
                    .Select(file => $"已添加 {file}")
                    .Concat(updatedFiles[syncPath.path].Select(file => $"已更新 {file}"))
                    .Concat(configDeleteRemovedFiles.Value || syncPath.enforced ? removedFiles[syncPath.path].Select(file => $"REMOVED {file}") : [])
                    .Concat(createdDirectories[syncPath.path].Select(file => $@"已更新 {file}\"))
            )
            .ToList();

    private List<string> _required;
    private List<string> required =>
        _required ??= EnabledSyncPaths
            .Where(syncPath => syncPath.enforced)
            .SelectMany(syncPath =>
                addedFiles[syncPath.path]
                    .Select(file => $"已添加 {file}")
                    .Concat(updatedFiles[syncPath.path].Select(file => $"已更新 {file}"))
                    .Concat(configDeleteRemovedFiles.Value ? removedFiles[syncPath.path].Select(file => $"已移除 {file}") : [])
                    .Concat(createdDirectories[syncPath.path].Select(file => $@"已更新 {file}\"))
            )
            .ToList();

    private List<string> _noRestart;
    private List<string> noRestart =>
        _noRestart ??= EnabledSyncPaths
            .Where(syncPath => !syncPath.restartRequired)
            .SelectMany(syncPath =>
                addedFiles[syncPath.path]
                    .Concat(updatedFiles[syncPath.path])
                    .Concat((configDeleteRemovedFiles.Value || syncPath.enforced) ? removedFiles[syncPath.path] : [])
                    .Concat(createdDirectories[syncPath.path])
            )
            .ToList();

    private void OnGUI()
    {
        if (!Singleton<CommonUI>.Instantiated)
            return;

        if (restartWindow.Active)
            restartWindow.Draw(StartUpdaterProcess);

        if (progressWindow.Active)
            progressWindow.Draw(downloadCount, totalDownloadCount, required.Count != 0 || noRestart.Count != 0 ? null : () => Task.Run(CancelUpdatingMods));

        if (updateWindow.Active)
        {
            updateWindow.Draw(
                (optional.Count != 0 ? string.Join("\n", optional) : "")
                    + (optional.Count != 0 && required.Count != 0 ? "\n\n" : "")
                    + (required.Count != 0 ? "[执行]\n" + string.Join("\n", required) : ""),
                () => Task.Run(() => SyncMods(addedFiles, updatedFiles, createdDirectories)),
                required.Count != 0 && optional.Count == 0 ? null : SkipUpdatingMods
            );
        }

        if (downloadErrorWindow.Active)
            downloadErrorWindow.Draw(Application.Quit);
    }

    public void Start()
    {
        StartCoroutine(StartPlugin());
    }

    public void Update()
    {
        if (updateWindow.Active || progressWindow.Active || restartWindow.Active || downloadErrorWindow.Active)
        {
            if (Singleton<LoginUI>.Instantiated && Singleton<LoginUI>.Instance.gameObject.activeSelf)
                Singleton<LoginUI>.Instance.gameObject.SetActive(false);

            if (Singleton<PreloaderUI>.Instantiated && Singleton<PreloaderUI>.Instance.gameObject.activeSelf)
                Singleton<PreloaderUI>.Instance.gameObject.SetActive(false);

            if (Singleton<CommonUI>.Instantiated && Singleton<CommonUI>.Instance.gameObject.activeSelf)
                Singleton<CommonUI>.Instance.gameObject.SetActive(false);
        }
        else if (pluginFinished)
        {
            pluginFinished = false;
            if (Singleton<LoginUI>.Instantiated && !Singleton<LoginUI>.Instance.gameObject.activeSelf)
                Singleton<LoginUI>.Instance.gameObject.SetActive(true);

            if (Singleton<PreloaderUI>.Instantiated && !Singleton<PreloaderUI>.Instance.gameObject.activeSelf)
                Singleton<PreloaderUI>.Instance.gameObject.SetActive(true);

            if (Singleton<CommonUI>.Instantiated && !Singleton<CommonUI>.Instance.gameObject.activeSelf)
                Singleton<CommonUI>.Instance.gameObject.SetActive(true);
        }
    }
}
