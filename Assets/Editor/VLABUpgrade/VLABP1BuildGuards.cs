using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace VLAB.Editor.Upgrade
{
    /// <summary>Removes editor/development-only simulator content from release scene copies.</summary>
    internal sealed class VLABP1ReleaseSceneGuard : IProcessSceneWithReport
    {
        private const string SimulatorRootName = "VLAB XR Interaction Simulator";
        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
                return;

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            if (development)
            {
                Debug.Log("[VLAB][P1.d3] Development build keeps the XR Interaction Simulator for test-only use.");
                return;
            }

            int stripped = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in transforms)
                {
                    if (transform != null && transform.name == SimulatorRootName)
                    {
                        UnityEngine.Object.DestroyImmediate(transform.gameObject);
                        stripped++;
                        break;
                    }
                }
            }

            bool simulatorComponentRemains = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component != null && (component.GetType().Name == "XRInteractionSimulator" || component.GetType().Name == "XRDeviceSimulator"));
            if (simulatorComponentRemains)
                throw new BuildFailedException("Release scene still contains XRInteractionSimulator after stripping.");

            Debug.Log($"[VLAB][P1.d3] Release guard stripped {stripped} simulator root(s) from scene copy '{scene.path}'.");
        }
    }

    /// <summary>Builds a Windows x64 player and optionally launches the self-terminating smoke probe.</summary>
    internal static class VLABP1WindowsBuildRunner
    {
        [Serializable]
        private sealed class WindowsBuildEvidence
        {
            public string generatedUtc;
            public string unityVersion;
            public string kind;
            public string result;
            public string outputPath;
            public ulong totalSizeBytes;
            public int totalErrors;
            public int totalWarnings;
            public int playerExitCode;
            public bool smokeArtifactExists;
        }

        public static void BuildAndSmokeWindows()
        {
            RunBuild(development: true, launchSmoke: true);
        }

        public static void BuildReleaseGuard()
        {
            RunBuild(development: false, launchSmoke: false);
        }

        public static void BuildReleaseAndSmokeWindows()
        {
            RunBuild(development: false, launchSmoke: true);
        }

        private static void RunBuild(bool development, bool launchSmoke)
        {
            string artifactRoot = Path.GetFullPath(RequireArgument("-vlabArtifactRoot"));
            if (Directory.Exists(artifactRoot) && Directory.EnumerateFileSystemEntries(artifactRoot).Any())
                throw new InvalidOperationException("Refusing to overwrite non-empty build evidence directory: " + artifactRoot);
            Directory.CreateDirectory(artifactRoot);

            string kind = development ? "development" : "release";
            string buildDirectory = Path.Combine(artifactRoot, "WindowsPlayer");
            string executablePath = Path.Combine(buildDirectory, "VLAB Chemistry Lab.exe");
            Directory.CreateDirectory(buildDirectory);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes are configured for the Windows player.");

            BuildOptions options = development ? BuildOptions.Development : BuildOptions.None;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });

            int playerExitCode = -1;
            string smokeArtifactPath = Path.Combine(artifactRoot, "player-smoke.json");
            if (report.summary.result == BuildResult.Succeeded && launchSmoke)
                playerExitCode = LaunchSmoke(executablePath, artifactRoot, smokeArtifactPath);

            WindowsBuildEvidence evidence = new WindowsBuildEvidence
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                kind = kind,
                result = report.summary.result.ToString(),
                outputPath = executablePath,
                totalSizeBytes = report.summary.totalSize,
                totalErrors = report.summary.totalErrors,
                totalWarnings = report.summary.totalWarnings,
                playerExitCode = playerExitCode,
                smokeArtifactExists = File.Exists(smokeArtifactPath)
            };
            File.WriteAllText(Path.Combine(artifactRoot, "windows-build-evidence.json"), JsonUtility.ToJson(evidence, true));
            File.WriteAllLines(Path.Combine(artifactRoot, "build-messages.txt"),
                report.steps.SelectMany(step => step.messages)
                    .Where(message => message.type == LogType.Warning || message.type == LogType.Error || message.type == LogType.Exception)
                    .Select(message => message.type + ": " + message.content));

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Windows {kind} build failed with {report.summary.totalErrors} error(s).");
            if (launchSmoke && (playerExitCode != 0 || !File.Exists(smokeArtifactPath)))
                throw new BuildFailedException($"Windows player smoke failed (exit {playerExitCode}, artifact present: {File.Exists(smokeArtifactPath)}).");

            Debug.Log($"[VLAB][P1.gate] Windows {kind} build {(launchSmoke ? "and player smoke " : string.Empty)}passed.");
        }

        private static int LaunchSmoke(string executablePath, string artifactRoot, string smokeArtifactPath)
        {
            string playerLogPath = Path.Combine(artifactRoot, "player.log");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = string.Join(" ", new[]
                {
                    "-screen-fullscreen", "0",
                    "-screen-width", "1280",
                    "-screen-height", "720",
                    "-vlabMode", "hardware",
                    "-vlabSmokeTest",
                    "-vlabSmokeScreenshot",
                    "-vlabSmokeExpected", "fallback",
                    "-vlabSmokeArtifact", Quote(smokeArtifactPath),
                    "-logFile", Quote(playerLogPath)
                }),
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new BuildFailedException("Could not start the Windows smoke player.");
                if (!process.WaitForExit(90000))
                {
                    process.Kill();
                    throw new BuildFailedException("Windows smoke player did not self-terminate within 90 seconds.");
                }
                return process.ExitCode;
            }
        }

        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

        private static string RequireArgument(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], key, StringComparison.OrdinalIgnoreCase))
                    return args[index + 1];
            }
            throw new InvalidOperationException("Missing required command line argument " + key);
        }
    }
}
