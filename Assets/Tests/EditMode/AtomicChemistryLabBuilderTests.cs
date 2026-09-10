using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class AtomicChemistryLabBuilderTests
    {
        private const string TargetScenePath = "Assets/ChemistryLab.unity";

        [TestCase("Preflight")]
        [TestCase("Build")]
        [TestCase("Validation")]
        [TestCase("Swap")]
        public void InjectedFailure_PreservesTargetSceneHash(string failurePoint)
        {
            string checkpointId = "atomic-test-" + failurePoint.ToLowerInvariant();
            string checkpointRoot = Path.GetFullPath(Path.Combine("Library", "VLABAtomicBuilderTests", checkpointId));
            string checkpointScene = Path.Combine(checkpointRoot, TargetScenePath.Replace('/', Path.DirectorySeparatorChar));
            string targetScene = Path.GetFullPath(TargetScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(checkpointScene));
            File.Copy(targetScene, checkpointScene, true);
            string before = Sha256(targetScene);

            try
            {
                MethodInfo method = GetRequiredBuilderMethod();
                TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(
                    () => method.Invoke(null, new object[] { checkpointId, checkpointRoot, failurePoint }));
                Assert.That(invocation.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(Sha256(targetScene), Is.EqualTo(before),
                    $"Injected {failurePoint} failure changed the target scene.");
            }
            finally
            {
                if (Directory.Exists(checkpointRoot))
                    Directory.Delete(checkpointRoot, true);
            }
        }

        [Test]
        public void MissingCheckpoint_IsRejectedBeforeTargetMutation()
        {
            string targetScene = Path.GetFullPath(TargetScenePath);
            string before = Sha256(targetScene);
            MethodInfo method = GetRequiredBuilderMethod();

            TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(
                () => method.Invoke(null, new object[] { "missing-checkpoint", Path.GetFullPath("Library/does-not-exist"), "None" }));

            Assert.That(invocation.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(Sha256(targetScene), Is.EqualTo(before));
        }

        private static MethodInfo GetRequiredBuilderMethod()
        {
            Type builder = Type.GetType("ChemistryLabBuilder, Assembly-CSharp-Editor", true);
            MethodInfo method = builder.GetMethod(
                "BuildChemistryLabForTests",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(builder.FullName, "BuildChemistryLabForTests");
            return method;
        }

        private static string Sha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
