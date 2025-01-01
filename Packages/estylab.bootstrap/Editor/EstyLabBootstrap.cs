using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace EstyLab.Bootstrap
{
    public class EstyLabBootstrap : MonoBehaviour
    {
        [MenuItem("EstyLab/Bootstrap/SetupPackages")]
        static void SetupPackages()
        {
            var packageList = LoadPackageList();
            var downloadedPackageList = GetDownloadedPackageList();

            foreach (var package in packageList)
            {
                if (!downloadedPackageList.Any(x => Path.GetFileNameWithoutExtension(x.FullName) == package))
                {
                    Debug.LogWarning($"Package not found: {package}");
                    continue;
                }

                var targetPackage = downloadedPackageList.First(x => Path.GetFileNameWithoutExtension(x.FullName) == package);

                Debug.Log($"Importing package: {targetPackage.FullName}");
                AssetDatabase.ImportPackage(targetPackage.FullName, false);
            }
        }

        [MenuItem("EstyLab/Bootstrap/ListPackages")]
        static void ListPackages()
        {
            var packageList = GetDownloadedPackageList();

            Debug.Log($"{packageList.Count()} packages found.");
            Debug.Log($"{string.Join("\n", packageList.Select(x => Path.GetFileNameWithoutExtension(x.FullName)))}");
        }

        private static IEnumerable<FileInfo> GetDownloadedPackageList()
        {
            var assetStoreDirectory = GetAssetStoreDirectory();
            if (!assetStoreDirectory.Exists)
            {
                Debug.LogWarning($"Asset Store directory not found: {assetStoreDirectory.FullName}");
                return new FileInfo[0];
            }

            return assetStoreDirectory.EnumerateFiles("*.unitypackage", SearchOption.AllDirectories);
        }

        [MenuItem("EstyLab/Bootstrap/CreateSymbolicLinks")]
        static void CreateSymbolicLinks()
        {
            var symbolicLinkList = GetSymbolicLinkList();

            foreach (var (source, target) in symbolicLinkList)
            {
                CreateSymbolicLink(new DirectoryInfo(source), new DirectoryInfo(Path.Combine(Application.dataPath, target)));
            }
        }

        private static void CreateSymbolicLink(DirectoryInfo source, DirectoryInfo target)
        {
            Debug.Log($"Creating symbolic link: {source.FullName} -> {target.FullName}");

            if (!source.Exists)
            {
                Debug.LogError($"Source directory not found: {source.FullName}");
                return;
            }

            if (target.Exists)
            {
                Debug.LogError($"Target directory is already exist: {target.FullName}");
                return;
            }

            EnsureDirectory(target.Parent);
            
            CreateJunctionWindows(source.FullName, target.FullName);
        }

        private static void EnsureDirectory(DirectoryInfo target)
        {
            if (!target.Parent.Exists)
            {
                EnsureDirectory(target.Parent);
            }

            Directory.CreateDirectory(target.FullName);
        }

        private static void CreateJunctionWindows(string source, string target)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C mklink /J \"{target}\" \"{source}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Debug.Log(output);
            Debug.Log(error);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"ジャンクションの作成に失敗しました: {error}");
            }
        }

        private static IEnumerable<(string source, string target)> GetSymbolicLinkList()
        {
            var homeDirectory = GetHomeDirectory();
            YamlDotNet.Serialization.Deserializer deserializer = new YamlDotNet.Serialization.Deserializer();

            var symbolicLinkListPath = Path.Combine(homeDirectory.FullName, ".config/VRChat.SymbolicLinkList.txt");
            if (!File.Exists(symbolicLinkListPath))
            {
                Debug.LogWarning($"Symbolic link list not found: {symbolicLinkListPath}");
                return new (string, string)[0];
            }

            Debug.Log($"Loading symbolic link list: {symbolicLinkListPath}");

            var symbolicLinkList = deserializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(symbolicLinkListPath));

            return symbolicLinkList.Select(x => (
                source: x.Value,
                target: x.Key)
            );
        }

        private static DirectoryInfo GetAssetStoreDirectory()
        {
            var roamingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var assetStoreDirectory = Path.Combine(roamingDirectory, "Unity", "Asset Store-5.x");
            return new DirectoryInfo(assetStoreDirectory);
        }

        private static IEnumerable<string> LoadPackageList()
        {
            var packageListPath = GetPackageListPath();
            if (!packageListPath.Exists)
            {
                Debug.LogWarning($"Package list not found: {packageListPath.FullName}");
                return new string[0];
            }

            Debug.Log($"Loading package list: {packageListPath.FullName}");

            return File.ReadAllText(packageListPath.FullName).Replace("\r\n", "\n").Split('\n').Where(x => !string.IsNullOrEmpty(x));
        }

        private static FileInfo GetPackageListPath()
        {
            var homeDirectory = GetHomeDirectory();
            var packageListPath = Path.Combine(homeDirectory.FullName, ".config/VRChat.PackageList.txt");
            return new FileInfo(packageListPath);
        }

        private static DirectoryInfo GetHomeDirectory()
        {
            var envHome = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(envHome))
            {
                return new DirectoryInfo(envHome);
            }
            else
            {
                return new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
        }

        private static void InstallPackage(string package)
        {
            Debug.Log($"Installing package: {package}");
        }
    }
}
