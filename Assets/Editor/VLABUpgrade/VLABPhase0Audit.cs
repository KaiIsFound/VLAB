using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VLAB.Editor.Upgrade
{
    /// <summary>Read-only Phase 0 inventory and deterministic baseline capture.</summary>
    internal static class VLABPhase0Audit
    {
        private const string ScenePath = "Assets/ChemistryLab.unity";
        private const string PlanVersion = "1.3";
        private const string DefaultOutputBase = "Artifacts/VLABUpgrade/plan-1.3/P0";

        [Serializable]
        private sealed class AuditReport
        {
            public string planVersion;
            public string startUtc;
            public string endUtc;
            public string generatedUtc;
            public string commandLine;
            public string sceneGuid;
            public string unityVersion;
            public string operatingSystem;
            public string processor;
            public string graphicsDevice;
            public int graphicsMemoryMb;
            public string colorSpace;
            public string activeQuality;
            public int msaa;
            public float shadowDistance;
            public string shadowResolution;
            public string graphicsDefaultPipeline;
            public string qualityPipeline;
            public string effectivePipeline;
            public bool chemistrySceneInBuild;
            public int generatedRootCount;
            public bool generatedV8MarkerPresent;
            public int totalGameObjects;
            public int missingScripts;
            public int renderers;
            public int missingMaterialSlots;
            public int materialsWithMissingOrUnsupportedShader;
            public int colliders;
            public int rigidbodies;
            public int cameras;
            public int activeEnabledCameras;
            public int mainCameras;
            public int audioListeners;
            public int activeEnabledAudioListeners;
            public int lights;
            public int realtimeLights;
            public int shadowCastingLights;
            public int reflectionProbes;
            public int lightProbes;
            public int canvases;
            public int textMeshLabels;
            public int tmpLabels;
            public int audioSources;
            public int particleSystems;
            public int xrNamedComponents;
            public int interactableNamedComponents;
            public int directInteractorNamedComponents;
            public int rayInteractorNamedComponents;
            public int teleportNamedComponents;
            public int locomotionNamedComponents;
            public int objectsOutsideFloorBounds;
            public string[] xrComponentTypes;
            public ShaderUsage[] sceneShaderUsage;
            public QualityEntry[] qualities;
            public QualityEntry[] qualitiesBefore;
            public QualityEntry[] qualitiesAfter;
            public bool rendererSentinelPass;
            public string[] screenshotFiles;
            public string[] acceptedLimitations;
        }

        [Serializable]
        private sealed class ShaderUsage
        {
            public string shader;
            public int materialSlots;
        }

        [Serializable]
        private sealed class QualityEntry
        {
            public string name;
            public int msaa;
            public float shadowDistance;
            public string pipeline;
        }

        private readonly struct CapturePoint
        {
            public readonly string name;
            public readonly Vector3 position;
            public readonly Vector3 target;
            public readonly float fieldOfView;

            public CapturePoint(string name, Vector3 position, Vector3 target, float fieldOfView = 55f)
            {
                this.name = name;
                this.position = position;
                this.target = target;
                this.fieldOfView = fieldOfView;
            }
        }

        [MenuItem("VLAB/Upgrade/Run Phase 0 Read-Only Audit")]
        public static void RunInteractive()
        {
            string outputRoot = ResolveOutputRoot();
            RunAudit(outputRoot);
            EditorUtility.RevealInFinder(Path.GetFullPath(outputRoot));
        }

        public static void RunBatch()
        {
            try
            {
                RunAudit(ResolveOutputRoot());
                Debug.Log("[VLAB][P0] Read-only audit and baseline capture completed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        public static void RunGuiAutomation()
        {
            try
            {
                RunAudit(ResolveOutputRoot());
                Debug.Log("[VLAB][P0] GUI automation audit and baseline capture completed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void RunLegacyValidationGui()
        {
            try
            {
                global::ChemistryLabEditorValidation.Validate();
                Debug.Log("[VLAB][P0] Existing ChemistryLab editor validation completed without rebuilding the scene.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RunAudit(string outputRoot)
        {
            PrepareImmutableOutput(outputRoot);
            DateTime startedUtc = DateTime.UtcNow;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("ChemistryLab scene could not be loaded for the Phase 0 audit.");

            List<GameObject> objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToList();
            List<Component> components = objects.SelectMany(item => item.GetComponents<Component>().Where(component => component != null)).ToList();
            Renderer[] renderers = objects.SelectMany(item => item.GetComponents<Renderer>()).ToArray();
            Camera[] cameras = objects.SelectMany(item => item.GetComponents<Camera>()).ToArray();
            AudioListener[] listeners = objects.SelectMany(item => item.GetComponents<AudioListener>()).ToArray();
            Light[] lights = objects.SelectMany(item => item.GetComponents<Light>()).ToArray();

            int missingScripts = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            int missingMaterialSlots = 0;
            HashSet<Material> materials = new HashSet<Material>();
            Dictionary<string, int> shaderUsage = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missingMaterialSlots++;
                        continue;
                    }

                    materials.Add(material);
                    string shaderName = material.shader == null ? "<missing>" : material.shader.name;
                    shaderUsage.TryGetValue(shaderName, out int count);
                    shaderUsage[shaderName] = count + 1;
                }
            }

            string[] xrTypeNames = components
                .Select(component => component.GetType().FullName ?? component.GetType().Name)
                .Where(typeName => typeName.Contains("XR", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(typeName => typeName, StringComparer.Ordinal)
                .ToArray();

            string graphicsPipelineBefore = PipelineName(GraphicsSettings.defaultRenderPipeline);
            string qualityPipelineBefore = PipelineName(QualitySettings.renderPipeline);
            string effectivePipelineBefore = PipelineName(GraphicsSettings.currentRenderPipeline);
            QualityEntry[] qualitiesBefore = CaptureQualityEntries();
            string[] screenshotFiles = CaptureBaselineImages(outputRoot);
            string graphicsPipelineAfter = PipelineName(GraphicsSettings.defaultRenderPipeline);
            string qualityPipelineAfter = PipelineName(QualitySettings.renderPipeline);
            string effectivePipelineAfter = PipelineName(GraphicsSettings.currentRenderPipeline);
            QualityEntry[] qualitiesAfter = CaptureQualityEntries();
            bool rendererSentinelPass = RendererSnapshotsMatch(
                graphicsPipelineBefore,
                qualityPipelineBefore,
                effectivePipelineBefore,
                qualitiesBefore,
                graphicsPipelineAfter,
                qualityPipelineAfter,
                effectivePipelineAfter,
                qualitiesAfter);
            if (!rendererSentinelPass)
                throw new InvalidOperationException("Renderer sentinel changed during the Phase 0 audit or an SRP became active.");

            AuditReport report = new AuditReport
            {
                planVersion = PlanVersion,
                startUtc = startedUtc.ToString("O"),
                endUtc = DateTime.UtcNow.ToString("O"),
                generatedUtc = DateTime.UtcNow.ToString("O"),
                commandLine = string.Join(" ", Environment.GetCommandLineArgs()),
                sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath),
                unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                colorSpace = PlayerSettings.colorSpace.ToString(),
                activeQuality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                msaa = QualitySettings.antiAliasing,
                shadowDistance = QualitySettings.shadowDistance,
                shadowResolution = QualitySettings.shadowResolution.ToString(),
                graphicsDefaultPipeline = graphicsPipelineAfter,
                qualityPipeline = qualityPipelineAfter,
                effectivePipeline = effectivePipelineAfter,
                chemistrySceneInBuild = EditorBuildSettings.scenes.Any(item => item.enabled && item.path == ScenePath),
                generatedRootCount = scene.GetRootGameObjects().Count(item => item.name == "__ChemistryLab_Generated__"),
                generatedV8MarkerPresent = objects.Any(item => item.name == "__ChemistryLab_Generated_v8"),
                totalGameObjects = objects.Count,
                missingScripts = missingScripts,
                renderers = renderers.Length,
                missingMaterialSlots = missingMaterialSlots,
                materialsWithMissingOrUnsupportedShader = materials.Count(material => material.shader == null || !material.shader.isSupported),
                colliders = components.Count(component => component is Collider),
                rigidbodies = components.Count(component => component is Rigidbody),
                cameras = cameras.Length,
                activeEnabledCameras = cameras.Count(camera => camera.enabled && camera.gameObject.activeInHierarchy),
                mainCameras = cameras.Count(camera => camera.CompareTag("MainCamera")),
                audioListeners = listeners.Length,
                activeEnabledAudioListeners = listeners.Count(listener => listener.enabled && listener.gameObject.activeInHierarchy),
                lights = lights.Length,
                realtimeLights = lights.Count(light => light.lightmapBakeType == LightmapBakeType.Realtime),
                shadowCastingLights = lights.Count(light => light.shadows != LightShadows.None),
                reflectionProbes = components.Count(component => component is ReflectionProbe),
                lightProbes = LightmapSettings.lightProbes == null ? 0 : LightmapSettings.lightProbes.count,
                canvases = components.Count(component => component is Canvas),
                textMeshLabels = components.Count(component => component is TextMesh),
                tmpLabels = components.Count(component => component.GetType().FullName != null && component.GetType().FullName.StartsWith("TMPro.", StringComparison.Ordinal)),
                audioSources = components.Count(component => component is AudioSource),
                particleSystems = components.Count(component => component is ParticleSystem),
                xrNamedComponents = CountTypes(components, "XR"),
                interactableNamedComponents = CountTypes(components, "Interactable"),
                directInteractorNamedComponents = CountTypes(components, "DirectInteractor"),
                rayInteractorNamedComponents = CountTypes(components, "RayInteractor"),
                teleportNamedComponents = CountTypes(components, "Teleport"),
                locomotionNamedComponents = CountTypes(components, "Locomotion"),
                objectsOutsideFloorBounds = objects.Count(item => item.transform.position.x < -8f || item.transform.position.x > 8f || item.transform.position.z < -8f || item.transform.position.z > 8f),
                xrComponentTypes = xrTypeNames,
                sceneShaderUsage = shaderUsage.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ShaderUsage { shader = pair.Key, materialSlots = pair.Value }).ToArray(),
                qualities = qualitiesAfter,
                qualitiesBefore = qualitiesBefore,
                qualitiesAfter = qualitiesAfter,
                rendererSentinelPass = rendererSentinelPass,
                screenshotFiles = screenshotFiles,
                acceptedLimitations = new[]
                {
                    "Editor camera rendering is structural/screenshot evidence, not headset visual evidence.",
                    "Draw calls, CPU/GPU frame time and GC allocation require a running player/profiler and are not inferred here.",
                    "OpenXR hardware tracking, stereo, haptic and comfort require a headset."
                }
            };

            File.WriteAllText(Path.Combine(outputRoot, "phase0-audit.json"), JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
        }

        private static string ResolveOutputRoot()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], "-vlabArtifactRoot", StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(args[index + 1]);
            }

            string runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return Path.GetFullPath(Path.Combine(DefaultOutputBase, runId));
        }

        private static void PrepareImmutableOutput(string outputRoot)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
                throw new ArgumentException("A Phase 0 artifact root is required.", nameof(outputRoot));

            string auditPath = Path.Combine(outputRoot, "phase0-audit.json");
            string screenshotPath = Path.Combine(outputRoot, "baseline-screenshots");
            if (File.Exists(auditPath) || Directory.Exists(screenshotPath))
                throw new InvalidOperationException("Refusing to overwrite an existing Phase 0 evidence run: " + outputRoot);

            Directory.CreateDirectory(outputRoot);
        }

        private static bool RendererSnapshotsMatch(
            string graphicsBefore,
            string qualityBefore,
            string effectiveBefore,
            QualityEntry[] qualitiesBefore,
            string graphicsAfter,
            string qualityAfter,
            string effectiveAfter,
            QualityEntry[] qualitiesAfter)
        {
            const string builtIn = "Built-in Render Pipeline";
            if (graphicsBefore != builtIn || qualityBefore != builtIn || effectiveBefore != builtIn ||
                graphicsAfter != builtIn || qualityAfter != builtIn || effectiveAfter != builtIn ||
                qualitiesBefore.Length != qualitiesAfter.Length)
                return false;

            for (int index = 0; index < qualitiesBefore.Length; index++)
            {
                QualityEntry before = qualitiesBefore[index];
                QualityEntry after = qualitiesAfter[index];
                if (before.name != after.name || before.pipeline != builtIn || after.pipeline != builtIn ||
                    before.msaa != after.msaa || !Mathf.Approximately(before.shadowDistance, after.shadowDistance))
                    return false;
            }

            return true;
        }

        private static int CountTypes(IEnumerable<Component> components, string fragment)
        {
            return components.Count(component => (component.GetType().FullName ?? component.GetType().Name)
                .Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }

        private static string PipelineName(RenderPipelineAsset pipeline)
        {
            return pipeline == null ? "Built-in Render Pipeline" : pipeline.GetType().FullName + " | " + pipeline.name;
        }

        private static QualityEntry[] CaptureQualityEntries()
        {
            int original = QualitySettings.GetQualityLevel();
            List<QualityEntry> entries = new List<QualityEntry>();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    entries.Add(new QualityEntry
                    {
                        name = QualitySettings.names[index],
                        msaa = QualitySettings.antiAliasing,
                        shadowDistance = QualitySettings.shadowDistance,
                        pipeline = PipelineName(QualitySettings.renderPipeline)
                    });
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(original, false);
            }
            return entries.ToArray();
        }

        private static string[] CaptureBaselineImages(string outputRoot)
        {
            CapturePoint[] points =
            {
                new CapturePoint("EntranceOverview", new Vector3(0f, 1.70f, -6.4f), new Vector3(0f, 1.25f, .8f), 55f),
                new CapturePoint("TitrationStanding", new Vector3(0f, 1.70f, -2.2f), new Vector3(0f, 1.35f, .58f), 48f),
                new CapturePoint("TitrationSeated", new Vector3(0f, 1.08f, -2.0f), new Vector3(0f, 1.18f, .58f), 48f),
                new CapturePoint("DaniellFront", new Vector3(-4.70f, 1.55f, 3.25f), new Vector3(-4.70f, 1.28f, 5.20f), 46f),
                new CapturePoint("ElectrolysisFront", new Vector3(4.70f, 1.55f, 3.25f), new Vector3(4.70f, 1.28f, 5.20f), 46f),
                new CapturePoint("ReagentCloseup", new Vector3(-4.70f, 1.42f, 2.75f), new Vector3(-4.70f, 1.08f, 3.85f), 42f),
                new CapturePoint("BottleLabelCloseup", new Vector3(-4.70f, 1.20f, 3.10f), new Vector3(-4.70f, 1.08f, 3.85f), 34f),
                new CapturePoint("ButtonLabelStanding", new Vector3(0f, 1.58f, -2.15f), new Vector3(0f, 1.08f, -.55f), 42f),
                new CapturePoint("InstructionPanelVRDistance", new Vector3(0f, 1.60f, -1.70f), new Vector3(0f, 1.22f, -.40f), 50f),
                new CapturePoint("BrightBackgroundText", new Vector3(0f, 2.45f, 4.8f), new Vector3(0f, 2.70f, 7.75f), 48f),
                new CapturePoint("DarkBackgroundText", new Vector3(-4.70f, 1.55f, 3.65f), new Vector3(-4.70f, 1.58f, 4.85f), 40f)
            };

            string captureDirectory = Path.Combine(outputRoot, "baseline-screenshots");
            Directory.CreateDirectory(captureDirectory);
            GameObject cameraObject = new GameObject("__VLAB_Phase0_CaptureCamera__") { hideFlags = HideFlags.HideAndDontSave };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .16f, .22f);
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 100f;

            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            List<string> files = new List<string>();
            try
            {
                camera.targetTexture = target;
                foreach (CapturePoint point in points)
                {
                    camera.transform.position = point.position;
                    camera.transform.rotation = Quaternion.LookRotation(point.target - point.position, Vector3.up);
                    camera.fieldOfView = point.fieldOfView;
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply(false, false);
                    string file = Path.Combine(captureDirectory, point.name + ".png");
                    File.WriteAllBytes(file, image.EncodeToPNG());
                    files.Add(file.Replace('\\', '/'));
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            return files.ToArray();
        }
    }
}
