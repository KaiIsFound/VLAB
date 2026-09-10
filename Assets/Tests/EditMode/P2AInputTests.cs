using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VLAB.ChemistryLab.Input;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class P2AInputTests
    {
        private const string DesktopActionsPath =
            "Assets/VLABChemistryLab/Input/Resources/VLAB Desktop Input Actions.inputactions";

        [Test]
        public void SemanticFrame_NormalizesDiagonalMovementAndBlocksZoomOverScrollableUi()
        {
            VLabDesktopInputFrame frame = new VLabDesktopInputFrame
            {
                Move = new Vector2(1f, 1f),
                ZoomDelta = 2f,
                PointerOverScrollableUi = true
            };

            Assert.That(VLabDesktopInputPolicy.NormalizeMove(frame.Move).magnitude, Is.EqualTo(1f).Within(.0001f));
            Assert.That(VLabDesktopInputPolicy.FilterZoom(frame), Is.Zero);

            frame.PointerOverScrollableUi = false;
            Assert.That(VLabDesktopInputPolicy.FilterZoom(frame), Is.EqualTo(2f));
        }

        [Test]
        public void DesktopInputAsset_DefinesTheRequiredSemanticActionsAndBindings()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DesktopActionsPath);
            Assert.That(asset, Is.Not.Null);

            InputActionMap map = asset.FindActionMap("Desktop", true);
            AssertBinding(map, "Move", "<Keyboard>/w");
            AssertBinding(map, "Move", "<Keyboard>/a");
            AssertBinding(map, "Move", "<Keyboard>/s");
            AssertBinding(map, "Move", "<Keyboard>/d");
            AssertBinding(map, "Look", "<Mouse>/delta");
            AssertBinding(map, "LookHold", "<Mouse>/rightButton");
            AssertBinding(map, "Run", "<Keyboard>/leftShift");
            AssertBinding(map, "Crouch", "<Keyboard>/c");
            AssertBinding(map, "Zoom", "<Mouse>/scroll/y");
            AssertBinding(map, "Interact", "<Mouse>/leftButton");
            AssertBinding(map, "Cancel", "<Keyboard>/escape");
        }

        [Test]
        public void DesktopHotPath_DoesNotPollTheLegacyInputManager()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string navigator = File.ReadAllText(Path.Combine(projectRoot,
                "Assets", "VLABChemistryLab", "Scripts", "DesktopLabNavigator.cs"));
            string hands = File.ReadAllText(Path.Combine(projectRoot,
                "Assets", "VLABChemistryLab", "Scripts", "DesktopLabHands.cs"));

            StringAssert.DoesNotContain("Input.Get", navigator);
            StringAssert.DoesNotContain("Input.mouse", navigator);
            StringAssert.DoesNotContain("Input.Get", hands);
            StringAssert.DoesNotContain("Input.mouse", hands);
        }

        private static void AssertBinding(InputActionMap map, string actionName, string effectivePath)
        {
            InputAction action = map.FindAction(actionName, true);
            Assert.That(action.bindings, Has.Some.Matches<InputBinding>(binding =>
                string.Equals(binding.effectivePath, effectivePath, System.StringComparison.OrdinalIgnoreCase)),
                $"{actionName} must include {effectivePath}.");
        }
    }
}
