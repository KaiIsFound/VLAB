using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VLAB.ChemistryLab.Mode;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public sealed class PlayModeBootstrapTests
    {
        [UnityTest]
        public IEnumerator TestRunner_EntersPlayModeAndAdvancesOneFrame()
        {
            Assert.That(Application.isPlaying, Is.True, "This suite must execute through the PlayMode runner.");

            GameObject sentinel = new GameObject("__VLAB_PlayMode_Bootstrap_Sentinel__");
            try
            {
                yield return null;
                Assert.That(sentinel, Is.Not.Null);
                Assert.That(sentinel.activeInHierarchy, Is.True);
            }
            finally
            {
                Object.Destroy(sentinel);
            }
        }

        [UnityTest]
        public IEnumerator PlayerInputHandling_RemainsBothDuringPhase1Bootstrap()
        {
            yield return null;
            string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "ProjectSettings.asset");
            string settings = File.ReadAllText(settingsPath);
            StringAssert.Contains("activeInputHandler: 2", settings,
                "Phase 1 must preserve Both input backends until the Desktop Input Actions migration has passed.");
        }

        [UnityTest]
        public IEnumerator ChemistryLabScene_SwitchesDesktopSimulatorAndHardwarePresentationSafely()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("ChemistryLab", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "ChemistryLab must remain in Build Settings for the Windows smoke path.");
            yield return load;
            yield return null;

            ChemistryLabModeController controller = Object.FindAnyObjectByType<ChemistryLabModeController>();
            Assert.That(controller, Is.Not.Null, "The generated scene must own a presentation mode controller.");

            GameObject xrOrigin = FindSceneObject("VLAB XR Origin");
            GameObject simulator = FindSceneObject("VLAB XR Interaction Simulator");
            Assert.That(xrOrigin, Is.Not.Null);
            Assert.That(simulator, Is.Not.Null);
            Assert.That(simulator.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(), Is.Not.Null,
                "The scene must use the requested package XR Device Simulator, not decorative Desktop hands.");

            AssertPresentation(controller, VLabPresentationMode.Desktop, xrOrigin, simulator,
                expectXr: false, expectSimulator: false);

            controller.ApplyPresentationMode(VLabPresentationMode.XRSimulator);
            yield return null;
            AssertPresentation(controller, VLabPresentationMode.XRSimulator, xrOrigin, simulator,
                expectXr: true, expectSimulator: true);

            controller.ApplyPresentationMode(VLabPresentationMode.OpenXRHardware);
            yield return null;
            AssertPresentation(controller, VLabPresentationMode.OpenXRHardware, xrOrigin, simulator,
                expectXr: true, expectSimulator: false);

            controller.ApplyPresentationMode(VLabPresentationMode.Desktop);
            yield return null;
            AssertPresentation(controller, VLabPresentationMode.Desktop, xrOrigin, simulator,
                expectXr: false, expectSimulator: false);
        }

        private static void AssertPresentation(
            ChemistryLabModeController controller,
            VLabPresentationMode expectedMode,
            GameObject xrOrigin,
            GameObject simulator,
            bool expectXr,
            bool expectSimulator)
        {
            Assert.That(controller.CurrentMode, Is.EqualTo(expectedMode));
            Assert.That(xrOrigin.activeSelf, Is.EqualTo(expectXr));
            Assert.That(simulator.activeSelf, Is.EqualTo(expectSimulator));

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            Assert.That(System.Array.FindAll(cameras, camera => camera.isActiveAndEnabled).Length, Is.EqualTo(1),
                $"{expectedMode} must expose exactly one active camera.");
            Assert.That(System.Array.FindAll(listeners, listener => listener.isActiveAndEnabled).Length, Is.EqualTo(1),
                $"{expectedMode} must expose exactly one active audio listener.");
            Assert.That(System.Array.FindAll(cameras, camera => camera.CompareTag("MainCamera")).Length, Is.EqualTo(1),
                $"{expectedMode} must tag exactly one owned camera as MainCamera.");
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            Transform match = System.Array.Find(transforms,
                item => item.gameObject.scene.IsValid() && item.name == objectName);
            return match == null ? null : match.gameObject;
        }
    }
}
