using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VLAB.ChemistryLab.Mode;
using System.Linq;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class P1D1RigTests
    {
        private const string RigPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Origin.prefab";
        private const string SimulatorPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Interaction Simulator.prefab";

        [Test]
        public void OwnedRigAndSimulatorAssets_SatisfyStructuralContract()
        {
            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            GameObject simulator = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            Assert.That(rig, Is.Not.Null);
            Assert.That(simulator, Is.Not.Null);
            Assert.That(rig.transform.localScale, Is.EqualTo(Vector3.one));
            MonoBehaviour[] rigComponents = rig.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(rigComponents.Count(item => item != null && item.GetType().Name == "XRDirectInteractor"), Is.EqualTo(2));
            Assert.That(rigComponents.Count(item => item is XRTrackedControllerGuard), Is.EqualTo(2));
            Assert.That(simulator.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(item => item != null && item.GetType().Name == "XRDeviceSimulator"), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(AssetDatabase.LoadMainAssetAtPath(RigPrefabPath)),
                Does.StartWith("Assets/VLABChemistryLab/"));
            Assert.That(AssetDatabase.GetAssetPath(AssetDatabase.LoadMainAssetAtPath(SimulatorPrefabPath)),
                Does.StartWith("Assets/VLABChemistryLab/"));
        }

        [Test]
        public void ModeController_EnforcesOneCameraListenerAndMainCameraTag()
        {
            GameObject host = new GameObject("Mode Host");
            GameObject desktop = CreateCamera("Desktop");
            GameObject xr = new GameObject("XR Origin");
            GameObject xrCameraObject = CreateCamera("XR Camera");
            xrCameraObject.transform.SetParent(xr.transform, false);
            GameObject manager = new GameObject("XR Interaction Manager");
            GameObject simulator = new GameObject("XR Interaction Simulator");
            DummyDesktopInterface desktopUi = host.AddComponent<DummyDesktopInterface>();
            ChemistryLabModeController controller = host.AddComponent<ChemistryLabModeController>();
            controller.Configure(desktop, desktopUi, xr, xrCameraObject.GetComponent<Camera>(), manager, simulator);

            try
            {
                controller.ApplyPresentationMode(VLabPresentationMode.Desktop);
                AssertMode(desktop.GetComponent<Camera>(), xrCameraObject.GetComponent<Camera>(), false, simulator);

                controller.ApplyPresentationMode(VLabPresentationMode.XRSimulator);
                AssertMode(desktop.GetComponent<Camera>(), xrCameraObject.GetComponent<Camera>(), true, simulator);

                controller.ApplyPresentationMode(VLabPresentationMode.OpenXRHardware);
                AssertMode(desktop.GetComponent<Camera>(), xrCameraObject.GetComponent<Camera>(), true, simulator, false);
                Assert.That(controller.CurrentMode, Is.EqualTo(VLabPresentationMode.OpenXRHardware));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(desktop);
                Object.DestroyImmediate(xr);
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(simulator);
            }
        }

        private static GameObject CreateCamera(string name)
        {
            GameObject target = new GameObject(name);
            target.AddComponent<Camera>();
            target.AddComponent<AudioListener>();
            return target;
        }

        private static void AssertMode(
            Camera desktop,
            Camera xr,
            bool xrExpected,
            GameObject simulator,
            bool? simulatorExpected = null)
        {
            Assert.That(desktop.enabled, Is.EqualTo(!xrExpected));
            Assert.That(xr.enabled, Is.EqualTo(xrExpected));
            Assert.That(desktop.GetComponent<AudioListener>().enabled, Is.EqualTo(!xrExpected));
            Assert.That(xr.GetComponent<AudioListener>().enabled, Is.EqualTo(xrExpected));
            int ownedMainCameraTags = (desktop.CompareTag("MainCamera") ? 1 : 0) +
                (xr.CompareTag("MainCamera") ? 1 : 0);
            Assert.That(ownedMainCameraTags, Is.EqualTo(1));
            Assert.That(simulator.activeSelf, Is.EqualTo(simulatorExpected ?? xrExpected));
        }

        private sealed class DummyDesktopInterface : MonoBehaviour { }
    }
}
