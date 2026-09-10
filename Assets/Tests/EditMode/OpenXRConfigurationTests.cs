using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class OpenXRConfigurationTests
    {
        [Test]
        public void Standalone_UsesOnlyOpenXrLoaderWithManualStartup()
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.InitManagerOnStart, Is.False);
            Assert.That(settings.Manager, Is.Not.Null);
            Assert.That(settings.Manager.activeLoaders.Count, Is.EqualTo(1));
            Assert.That(settings.Manager.activeLoaders[0], Is.TypeOf<OpenXRLoader>());
        }

        [Test]
        public void Standalone_EnablesOnlyReviewedControllerProfiles()
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            Assert.That(settings, Is.Not.Null);

            Type[] expected =
            {
                typeof(KHRSimpleControllerProfile),
                typeof(MicrosoftMotionControllerProfile),
                typeof(OculusTouchControllerProfile),
                typeof(ValveIndexControllerProfile)
            };
            Type[] enabled = settings.GetFeatures<OpenXRInteractionFeature>()
                .Where(feature => feature.enabled)
                .Select(feature => feature.GetType())
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            CollectionAssert.AreEquivalent(expected, enabled);
        }

        [Test]
        public void Standalone_HasNoMandatoryOpenXrValidationErrors()
        {
            List<OpenXRFeature.ValidationRule> issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Standalone);

            Assert.That(issues.Where(issue => issue.error).Select(issue => issue.message), Is.Empty);
        }

        [Test]
        public void PackageAndRendererInvariants_RemainPinned()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string manifest = File.ReadAllText(Path.Combine(projectRoot, "Packages", "manifest.json"));
            string packageLock = File.ReadAllText(Path.Combine(projectRoot, "Packages", "packages-lock.json"));
            StringAssert.Contains("\"com.unity.xr.management\": \"4.5.4\"", manifest);
            StringAssert.Contains("\"com.unity.xr.openxr\": \"1.17.1\"", manifest);
            StringAssert.Contains("\"com.unity.xr.interaction.toolkit\": {", packageLock);
            StringAssert.Contains("\"com.unity.inputsystem\": {", packageLock);
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Null);

            int originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    Assert.That(QualitySettings.renderPipeline, Is.Null, "Quality pipeline override changed for " + QualitySettings.names[index]);
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }
    }
}
