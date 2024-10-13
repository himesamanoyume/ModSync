using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ModSync;

public static class Sync
{
    public static Dictionary<string, List<string>> GetAddedFiles(
        List<SyncPath> syncPaths,
        Dictionary<string, Dictionary<string, ModFile>> localModFiles,
        Dictionary<string, Dictionary<string, ModFile>> remoteModFiles
    )
    {
        return syncPaths
            .Select(syncPath => new KeyValuePair<string, List<string>>(
                syncPath.path,
                remoteModFiles[syncPath.path]
                    .Where((kvp) => !kvp.Value.directory)
                    .Select((kvp) => kvp.Key)
                    .Except(localModFiles.TryGetValue(syncPath.path, out var modFiles) ? modFiles.Keys : new List<string>(), StringComparer.OrdinalIgnoreCase)
                    .ToList()
            ))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<string, List<string>> GetUpdatedFiles(
        List<SyncPath> syncPaths,
        Dictionary<string, Dictionary<string, ModFile>> localModFiles,
        Dictionary<string, Dictionary<string, ModFile>> remoteModFiles,
        Dictionary<string, Dictionary<string, ModFile>> previousRemoteModFiles
    )
    {
        return syncPaths
            .Select(syncPath =>
            {
                if (!localModFiles.TryGetValue(syncPath.path, out var localPathFiles))
                    return new KeyValuePair<string, List<string>>(syncPath.path, []);

                var query = remoteModFiles[syncPath.path].Keys.Intersect(localPathFiles.Keys, StringComparer.OrdinalIgnoreCase);

                if (!syncPath.enforced)
                    query = query.Where(file =>
                        !previousRemoteModFiles.TryGetValue(syncPath.path, out var previousPathFiles)
                        || !previousPathFiles.TryGetValue(file, out var modFile)
                        || remoteModFiles[syncPath.path][file].hash != modFile.hash
                    );

                query = query.Where(file => remoteModFiles[syncPath.path][file].hash != localPathFiles[file].hash);

                return new KeyValuePair<string, List<string>>(syncPath.path, query.ToList());
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<string, List<string>> GetRemovedFiles(
        List<SyncPath> syncPaths,
        Dictionary<string, Dictionary<string, ModFile>> localModFiles,
        Dictionary<string, Dictionary<string, ModFile>> remoteModFiles,
        Dictionary<string, Dictionary<string, ModFile>> previousRemoteModFiles
    )
    {
        return syncPaths
            .Select(syncPath =>
            {
                if (!localModFiles.TryGetValue(syncPath.path, out var localPathFiles))
                    return new KeyValuePair<string, List<string>>(syncPath.path, []);

                IEnumerable<string> query;
                if (syncPath.enforced)
                    query = localPathFiles.Keys.Except(remoteModFiles[syncPath.path].Keys, StringComparer.OrdinalIgnoreCase);
                else
                    query = !previousRemoteModFiles.TryGetValue(syncPath.path, out var previousPathFiles)
                        ? []
                        : previousPathFiles
                            .Keys.Intersect(localPathFiles.Keys, StringComparer.OrdinalIgnoreCase)
                            .Except(remoteModFiles[syncPath.path].Keys, StringComparer.OrdinalIgnoreCase);

                return new KeyValuePair<string, List<string>>(syncPath.path, query.ToList());
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<string, List<string>> GetCreatedDirectories(
        List<SyncPath> syncPaths,
        Dictionary<string, Dictionary<string, ModFile>> localModFiles,
        Dictionary<string, Dictionary<string, ModFile>> remoteModFiles
    )
    {
        return syncPaths
            .Select(syncPath =>
            {
                return new KeyValuePair<string, List<string>>(
                    syncPath.path,
                    remoteModFiles[syncPath.path]
                        .Where((kvp) => kvp.Value.directory)
                        .Select((kvp) => kvp.Key)
                        .Except(localModFiles[syncPath.path].Keys, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                );
            })
            .ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value);
    }

    private static List<string> GetFilesInDirectory(string basePath, string directory, List<Regex> exclusions)
    {
        if (File.Exists(directory))
            return [directory];

        if (!Directory.Exists(directory))
            return [];

        return Directory
            .GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where((file) => !IsExcluded(exclusions, file.Replace($"{basePath}\\", "")))
            .Concat(
                Directory
                    .GetDirectories(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where((subDir) => !IsExcluded(exclusions, subDir.Replace($"{basePath}\\", ""), basePath))
                    .SelectMany((subDir) => Directory.GetFileSystemEntries(subDir).Length == 0 ? [subDir] : GetFilesInDirectory(basePath, subDir, exclusions))
            )
            .ToList();
    }

    public static Dictionary<string, Dictionary<string, ModFile>> HashLocalFiles(
        string basePath,
        List<SyncPath> syncPaths,
        List<Regex> remoteExclusions,
        List<Regex> localExclusions
    )
    {
        var limitOpenFiles = new SemaphoreSlim(1024, 1024);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var results = syncPaths
            .Select(syncPath =>
            {
                var path = Path.Combine(basePath, syncPath.path);

                return new KeyValuePair<string, Dictionary<string, ModFile>>(
                    syncPath.path,
                    GetFilesInDirectory(basePath, path, remoteExclusions.Concat(syncPath.enforced ? [] : localExclusions).ToList())
                        .AsParallel()
                        .Select((file) => CreateModFile(basePath, file, limitOpenFiles))
                        .ToDictionary(kvp => kvp.Result.Key, kvp => kvp.Result.Value, StringComparer.OrdinalIgnoreCase)
                );
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        watch.Stop();
        Plugin.Logger.LogInfo($"Corter-ModSync: Hashing took {watch.Elapsed.TotalMilliseconds}ms");

        return results;
    }

    public static async Task<KeyValuePair<string, ModFile>> CreateModFile(string basePath, string file, SemaphoreSlim limiter)
    {
        var hash = "";
        var isDirectory = Directory.Exists(file);
        if (!isDirectory)
        {
            await limiter.WaitAsync();

            hash = await ImoHash.HashFile(file);

            limiter.Release();
        }

        var relativePath = file.Replace($"{basePath}\\", "");

        return new KeyValuePair<string, ModFile>(relativePath, new ModFile(hash, isDirectory));
    }

    public static void CompareModFiles(
        List<SyncPath> syncPaths,
        Dictionary<string, Dictionary<string, ModFile>> localModFiles,
        Dictionary<string, Dictionary<string, ModFile>> remoteModFiles,
        Dictionary<string, Dictionary<string, ModFile>> previousSync,
        out Dictionary<string, List<string>> addedFiles,
        out Dictionary<string, List<string>> updatedFiles,
        out Dictionary<string, List<string>> removedFiles,
        out Dictionary<string, List<string>> createdDirectories
    )
    {
        addedFiles = GetAddedFiles(syncPaths, localModFiles, remoteModFiles);
        updatedFiles = GetUpdatedFiles(syncPaths, localModFiles, remoteModFiles, previousSync);
        removedFiles = GetRemovedFiles(syncPaths, localModFiles, remoteModFiles, previousSync);
        createdDirectories = GetCreatedDirectories(syncPaths, localModFiles, remoteModFiles);
    }

    public static bool IsExcluded(List<Regex> exclusions, string path, string parent = null)
    {
        return exclusions.Any(regex => regex.IsMatch(path.Replace(@"\", "/")) && (parent == null || !regex.IsMatch(parent.Replace(@"\", "/"))));
    }
}
