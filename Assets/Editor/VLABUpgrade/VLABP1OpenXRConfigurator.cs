using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VLAB.Editor.Upgrade
{
    /// <summary>Creates the reviewed Windows OpenXR settings without editing serialized YAML by hand.</summary>
    internal static class VLABP1OpenXRConfigurator
    {
        private const string RequiredCheckpointId = "20260831-p1b-pre-openxr";
        private const string SettingsAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        [Serializable]
        private sealed class ConfigurationReport
        {
            public string planVersion;
            public string checkpointId;
            public string generatedUtc;
            public string unityVersion;
            public string buildTarget;
            public string buildTargetGroup;
            public bool initializeXrOnStartup;
            public bool openXrLoaderAssigned;
            public string[] assignedLoaders;
            public string[] enabledInteractionProfiles;
            public string[] disabledInteractionProfiles;
            public string xrGeneralSettingsAsset;
            public string openXrSettingsAsset;
            public ValidationEntry[] validationIssues;
            public int mandatoryErrorCount;
            public int warningCount;
        }

        [Serializable]
        private sealed class ValidationEntry
        {
            public string message;
            public bool error;
            public bool errorEnteringPlaymode;
            public bool hasFix;
            public bool fixIsAutomatic;
            public string fixDescription;
        }

        public static void ConfigureWindowsGui()
        {
            try
            {
                string outputRoot = Path.GetFullPath(RequireArgument("-vlabArtifactRoot"));
                string checkpointId = RequireArgument("-vlabCheckpointId");
                if (!string.Equals(checkpointId, RequiredCheckpointId, StringComparison.Ordinal))
                    throw new InvalidOperationException("P1.b configuration requires the verified checkpoint " + RequiredCheckpointId);
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 &&
                    EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows)
                    throw new InvalidOperationException("P1.b must configure from an active Windows Standalone build target.");

                Directory.CreateDirectory(outputRoot);
                string reportPath = Path.Combine(outputRoot, "openxr-configuration.json");
                if (File.Exists(reportPath))
                    throw new InvalidOperationException("Refusing to overwrite OpenXR configuration evidence: " + reportPath);

                const BuildTargetGroup group = BuildTargetGroup.Standalone;
                XRGeneralSettingsPerBuildTarget perBuildTarget = GetOrCreateGeneralSettings();
                if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
                    perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);

                XRGeneralSettings generalSettings = perBuildTarget.SettingsForBuildTarget(group);
                if (generalSettings == null || generalSettings.Manager == null)
                    throw new InvalidOperationException("Unable to create Standalone XR management settings.");

                generalSettings.InitManagerOnStart = false;
                EditorUtility.SetDirty(generalSettings);
                if (!XRPackageMetadataStore.AssignLoader(generalSettings.Manager, typeof(OpenXRLoader).FullName, group))
                    throw new InvalidOperationException("Unable to assign the OpenXR loader to Windows Standalone.");

                FeatureHelpers.RefreshFeatures(group);
                OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (openXrSettings == null)
                    throw new InvalidOperationException("Unable to create OpenXR settings for Windows Standalone.");

                Type[] desiredProfileTypes =
                {
                    typeof(KHRSimpleControllerProfile),
                    typeof(OculusTouchControllerProfile),
                    typeof(ValveIndexControllerProfile),
                    typeof(MicrosoftMotionControllerProfile)
                };
                HashSet<Type> desiredProfiles = new HashSet<Type>(desiredProfileTypes);
                List<string> enabledProfiles = new List<string>();
                List<string> disabledProfiles = new List<string>();
                foreach (OpenXRInteractionFeature profile in openXrSettings.GetFeatures<OpenXRInteractionFeature>())
                {
                    bool enable = desiredProfiles.Contains(profile.GetType());
                    profile.enabled = enable;
                    EditorUtility.SetDirty(profile);
                    (enable ? enabledProfiles : disabledProfiles).Add(profile.GetType().FullName);
                }

                foreach (Type desiredType in desiredProfileTypes)
                {
                    OpenXRFeature profile = openXrSettings.GetFeature(desiredType);
                    if (profile == null || !profile.enabled)
                        throw new InvalidOperationException("Required Windows interaction profile is unavailable: " + desiredType.FullName);
                }

                EditorUtility.SetDirty(perBuildTarget);
                EditorUtility.SetDirty(generalSettings.Manager);
                EditorUtility.SetDirty(openXrSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                List<OpenXRFeature.ValidationRule> issues = new List<OpenXRFeature.ValidationRule>();
                OpenXRProjectValidation.GetCurrentValidationIssues(issues, group);
                ValidationEntry[] validationEntries = issues.Select(issue => new ValidationEntry
                {
                    message = issue.message,
                    error = issue.error,
                    errorEnteringPlaymode = issue.errorEnteringPlaymode,
                    hasFix = issue.fixIt != null,
                    fixIsAutomatic = issue.fixItAutomatic,
                    fixDescription = issue.fixItMessage
                }).ToArray();

                string openXrAssetPath = AssetDatabase.GetAssetPath(openXrSettings);
                ConfigurationReport report = new ConfigurationReport
                {
                    planVersion = "1.3",
                    checkpointId = checkpointId,
                    generatedUtc = DateTime.UtcNow.ToString("O"),
                    unityVersion = Application.unityVersion,
                    buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    buildTargetGroup = group.ToString(),
                    initializeXrOnStartup = generalSettings.InitManagerOnStart,
                    openXrLoaderAssigned = generalSettings.Manager.activeLoaders.Any(loader => loader is OpenXRLoader),
                    assignedLoaders = generalSettings.Manager.activeLoaders.Where(loader => loader != null)
                        .Select(loader => loader.GetType().FullName).ToArray(),
                    enabledInteractionProfiles = enabledProfiles.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                    disabledInteractionProfiles = disabledProfiles.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                    xrGeneralSettingsAsset = AssetDatabase.GetAssetPath(perBuildTarget),
                    openXrSettingsAsset = openXrAssetPath,
                    validationIssues = validationEntries,
                    mandatoryErrorCount = validationEntries.Count(item => item.error),
                    warningCount = validationEntries.Count(item => !item.error)
                };
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
                Debug.Log("[VLAB][P1.b] Windows OpenXR configuration saved. Validation errors: " + report.mandatoryErrorCount + ", warnings: " + report.warningCount);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateGeneralSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget existing) && existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/XR"))
                AssetDatabase.CreateFolder("Assets", "XR");
            XRGeneralSettingsPerBuildTarget settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static string RequireArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[index + 1]))
                    return args[index + 1];
            }
            throw new ArgumentException(name + " is required.");
        }
    }
}
