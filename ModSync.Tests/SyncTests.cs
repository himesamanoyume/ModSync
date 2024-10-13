using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModSync.Tests;

[TestClass]
public class AddedFilesTests
{
    [TestMethod]
    public void TestSingleAdded()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") } }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var addedFiles = Sync.GetAddedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles);

        CollectionAssert.AreEqual(new List<string> { @"BepInEx\plugins\Corter-ModSync.dll" }, addedFiles[@"BepInEx\plugins"]);
    }

    [TestMethod]
    public void TestNoneAdded()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") } }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") }, }
            }
        };

        var addedFiles = Sync.GetAddedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles);

        Assert.AreEqual(0, addedFiles[@"BepInEx\plugins"].Count);
    }
}

[TestClass]
public class UpdatedFilesTests
{
    [TestMethod]
    public void TestSingleAdded()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") } }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") }, }
            }
        };

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        Assert.AreEqual(0, updatedFiles[@"BepInEx\plugins"].Count);
    }

    [TestMethod]
    public void TestSingleUpdated()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        CollectionAssert.AreEqual(new List<string> { @"BepInEx\plugins\Corter-ModSync.dll" }, updatedFiles[@"BepInEx\plugins"]);
    }

    [TestMethod]
    public void TestOnlyLocalUpdated()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        Assert.AreEqual(0, updatedFiles[@"BepInEx\plugins"].Count);
    }

    [TestMethod]
    public void TestFilesExistButPreviousEmpty()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                    { @"BepInEx\plugins\New-Mod.dll", new ModFile("1234567") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>();

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        Assert.AreEqual(1, updatedFiles[@"BepInEx\plugins"].Count);
        Assert.AreEqual(@"BepInEx\plugins\Corter-ModSync.dll", updatedFiles[@"BepInEx\plugins"][0]);
    }

    [TestMethod]
    public void TestBothUpdated()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        Assert.AreEqual(0, updatedFiles[@"BepInEx\plugins"].Count);
    }

    [TestMethod]
    public void TestSingleUpdatedEnforced()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("2345678") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("2345678") },
                }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var updatedFiles = Sync.GetUpdatedFiles([new SyncPath(@"BepInEx\plugins", enforced: true)], localModFiles, remoteModFiles, previousRemoteModFiles);

        Assert.AreEqual(2, updatedFiles[@"BepInEx\plugins"].Count);
        Assert.IsTrue(updatedFiles[@"BepInEx\plugins"].Contains(@"BepInEx\plugins\Corter-ModSync.dll"));
        Assert.IsTrue(updatedFiles[@"BepInEx\plugins"].Contains(@"BepInEx\plugins\SAIN\SAIN.dll"));
    }
}

[TestClass]
public class RemovedFilesTests
{
    [TestMethod]
    public void TestSingleRemoved()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") }, }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var removedFiles = Sync.GetRemovedFiles([new SyncPath(@"BepInEx\plugins")], localModFiles, remoteModFiles, previousRemoteModFiles);

        CollectionAssert.AreEqual(new List<string> { @"BepInEx\plugins\Corter-ModSync.dll" }, removedFiles[@"BepInEx\plugins"]);
    }

    [TestMethod]
    public void TestSingleRemovedEnforced()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\OtherPlugin\OtherPlugin.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567", true) },
                }
            }
        };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile> { { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") }, }
            }
        };

        var previousRemoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\SAIN\SAIN.dll", new ModFile("1234567") },
                    { @"BepInEx\plugins\Corter-ModSync.dll", new ModFile("1234567") },
                }
            }
        };

        var removedFiles = Sync.GetRemovedFiles([new SyncPath(@"BepInEx\plugins", enforced: true)], localModFiles, remoteModFiles, previousRemoteModFiles);

        CollectionAssert.AreEquivalent(
            new List<string> { @"BepInEx\plugins\Corter-ModSync.dll", @"BepInEx\plugins\OtherPlugin\OtherPlugin.dll" },
            removedFiles[@"BepInEx\plugins"]
        );
    }
}

[TestClass]
public class CreatedDirectoriesTests
{
    [TestMethod]
    public void TestCreatedDirectories()
    {
        var localModFiles = new Dictionary<string, Dictionary<string, ModFile>> { { @"BepInEx\plugins", new Dictionary<string, ModFile>() } };

        var remoteModFiles = new Dictionary<string, Dictionary<string, ModFile>>
        {
            {
                @"BepInEx\plugins",
                new Dictionary<string, ModFile>
                {
                    { @"BepInEx\plugins\ModThatDoesntErrorCheckFolders\SuperImportantEmptyFolder", new ModFile("1234567", directory: true) },
                }
            }
        };

        var createdDirectories = Sync.GetCreatedDirectories([new SyncPath(@"BepInEx\plugins", enforced: true)], localModFiles, remoteModFiles);

        CollectionAssert.AreEquivalent(
            new List<string> { @"BepInEx\plugins\ModThatDoesntErrorCheckFolders\SuperImportantEmptyFolder" },
            createdDirectories[@"BepInEx\plugins"]
        );
    }
}

[TestClass]
public class HashLocalFilesTests
{
    private readonly List<Regex> exclusions =
    [
        GlobRegex.GlobNoEnd("**/*.nosync"),
        GlobRegex.GlobNoEnd("**/*.nosync.txt"),
        GlobRegex.GlobNoEnd("plugins/file2.dll"),
        GlobRegex.GlobNoEnd("plugins/file3.dll"),
        GlobRegex.GlobNoEnd("plugins/ModName"),
        GlobRegex.GlobNoEnd("plugins/OtherMod/subdir"),
    ];

    private readonly Dictionary<string, string> fileContents =
        new()
        {
            { @"plugins\file1.dll", "Test content" },
            { @"plugins\file2.dll", "Test content 2" },
            { @"plugins\file2.dll.nosync", "" },
            { @"plugins\file3.dll", "Test content 3" },
            { @"plugins\file3.dll.nosync.txt", "" },
            { @"plugins\ModName\mod_name.dll", "Test content 4" },
            { @"plugins\ModName\.nosync", "" },
            { @"plugins\OtherMod\other_mod.dll", "Test content 5" },
            { @"plugins\OtherMod\subdir\image.png", "Test Image" },
            { @"plugins\OtherMod\subdir\.nosync", "" }
        };

    private string testDirectory;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = TestUtils.GetTemporaryDirectory();

        Directory.CreateDirectory(testDirectory);

        // Create test files
        foreach (var kvp in fileContents)
        {
            var filePath = Path.Combine(testDirectory, kvp.Key);
            var fileParent = Path.GetDirectoryName(filePath);

            if (fileParent != null && !Directory.Exists(fileParent))
                Directory.CreateDirectory(fileParent);

            File.WriteAllText(filePath, kvp.Value);
        }

        Console.WriteLine(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void TestHashLocalFiles()
    {
        var expected = fileContents.Where(kvp => !Sync.IsExcluded(exclusions, kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var result = Sync.HashLocalFiles(testDirectory, [new SyncPath("plugins")], exclusions, []);

        Assert.IsNotNull(result);

        foreach (var kvp in expected)
        {
            Assert.IsTrue(result["plugins"].ContainsKey(kvp.Key));
            Assert.AreEqual(ImoHash.HashFileObject(new MemoryStream(Encoding.ASCII.GetBytes(kvp.Value))).Result, result["plugins"][kvp.Key].hash);
        }

        Assert.AreEqual(2, result["plugins"].Count);
    }

    [TestMethod]
    public void TestHashLocalFilesWithDirectoryThatDoesNotExist()
    {
        var result = Sync.HashLocalFiles(testDirectory, [new SyncPath("bad_directory")], exclusions, []);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result["bad_directory"].Count);
    }

    [TestMethod]
    public void TestHashLocalFilesWithSingleFile()
    {
        var syncPath = Path.Combine(testDirectory, @"plugins\file1.dll");

        var result = Sync.HashLocalFiles(testDirectory, [new SyncPath(syncPath)], exclusions, []);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result[syncPath].Count);
        Assert.IsTrue(result[syncPath].ContainsKey(@"plugins\file1.dll"));
        Assert.AreEqual("0ce304b7ff04260d67adfdee0af9dd3b", result[syncPath][@"plugins\file1.dll"].hash);
    }

    [TestMethod]
    public void TestHashLocalFilesWithSingleFileThatDoesNotExist()
    {
        var syncPath = Path.Combine(testDirectory, "does_not_exist.dll");
        var result = Sync.HashLocalFiles(testDirectory, [new SyncPath(syncPath)], exclusions, []);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result[syncPath].Count);
    }

    [TestMethod]
    public void TestHashLocalFilesEnforcedIgnoresLocalExclusions()
    {
        var expected = fileContents.Where(kvp => !Sync.IsExcluded(exclusions, kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var result = Sync.HashLocalFiles(testDirectory, [new SyncPath("plugins", enforced: true)], exclusions, [GlobRegex.Glob("plugins/file1.dll")]);
        CollectionAssert.AreEquivalent(expected.Keys, result["plugins"].Keys);
    }
}

[TestClass]
public class CreateModFileTest
{
    private readonly Dictionary<string, string> fileContents =
        new()
        {
            { "file1.dll", "" },
            { "file2.dll", "" },
            { "file2.dll.nosync", "" },
            { "file3.dll", "Test content 3" },
            { "file3.dll.nosync.txt", "" },
            { @"ModName\mod_name.dll", "Test content 4" },
            { @"ModName\.nosync", "" },
            { @"OtherMod\other_mod.dll", "Test content 5" },
            { @"OtherMod\subdir\image.png", "Test Image" },
            { @"OtherMod\subdir\.nosync", "" }
        };

    private string testDirectory;
    private SemaphoreSlim limiter = new(1024, 1024);

    [TestInitialize]
    public void Setup()
    {
        testDirectory = TestUtils.GetTemporaryDirectory();

        Directory.CreateDirectory(testDirectory);

        // Create test files
        foreach (var kvp in fileContents)
        {
            var filePath = Path.Combine(testDirectory, kvp.Key);
            var fileParent = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(fileParent))
                Directory.CreateDirectory(fileParent);

            File.WriteAllText(filePath, kvp.Value);
        }

        Console.WriteLine(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void TestCreateModFile()
    {
        var kv = Sync.CreateModFile(testDirectory, Path.Combine(testDirectory, "file1.dll"), limiter);

        Assert.IsNotNull(kv.Result.Value);
        Assert.AreEqual("00d1413dcaf30500b65fc68446b10646", kv.Result.Value.hash);
    }

    [TestMethod]
    public void TestCreateModFileWithContent()
    {
        var kv = Sync.CreateModFile(testDirectory, Path.Combine(testDirectory, "file3.dll"), limiter);

        Assert.IsNotNull(kv.Result.Value);
        Assert.AreEqual("0e51ecd1fbd55148997270d6634ff6db", kv.Result.Value.hash);
    }
}

[TestClass]
public class IsExcludedTest
{
    private readonly Dictionary<string, string> fileContents =
        new()
        {
            { "file1.dll", "Test content" },
            { "file2.dll", "Test content 2" },
            { "file3.dll", "Test content 3" },
            { @"ModName\mod_name.dll", "Test content 4" },
            { @"ModName\.nosync", "" },
            { @"ModName\subdir\image.png", "Test Image 1" },
            { @"OtherMod\other_mod.dll", "Test content 5" },
            { @"OtherMod\subdir\image.png", "Test Image 2" },
            { @"OtherMod\subdir\.nosync", "" }
        };

    private readonly List<Regex> exclusions =
    [
        GlobRegex.Glob("**/*.nosync"),
        GlobRegex.Glob("**/*.nosync.txt"),
        GlobRegex.Glob("file2.dll"),
        GlobRegex.Glob("file3.dll"),
        GlobRegex.Glob(@"ModName"),
        GlobRegex.Glob(@"OtherMod\subdir"),
    ];

    private string testDirectory;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = TestUtils.GetTemporaryDirectory();

        Directory.CreateDirectory(testDirectory);

        // Create test files
        foreach (var kvp in fileContents)
        {
            var filePath = Path.Combine(testDirectory, kvp.Key);
            var fileParent = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(fileParent))
                Directory.CreateDirectory(fileParent);

            File.WriteAllText(filePath, kvp.Value);
        }

        Console.WriteLine(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void TestIsNotExcluded()
    {
        var result = Sync.IsExcluded(exclusions, "file1.dll");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TestIsExcluded()
    {
        var result = Sync.IsExcluded(exclusions, "file2.dll");
        Assert.IsTrue(result);
    }
}
