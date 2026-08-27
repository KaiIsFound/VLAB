using UnityEngine;

namespace VLAB.PhysicsLab
{
    /// <summary>
    /// Hardware-neutral entry point for the ESP32/BLE plugin. A native Android BLE plugin
    /// can invoke ReceiveCommand through UnitySendMessage without coupling lesson logic to it.
    /// </summary>
    public class VLabControllerBridge : MonoBehaviour
    {
        [SerializeField] private PendulumExperiment experiment;
        [SerializeField] private float lengthStep = 0.10f;

        public void Configure(PendulumExperiment target) => experiment = target;

        // Accepted values: start, stop, reset, clear, length_plus, length_minus.
        public void ReceiveCommand(string command)
        {
            if (experiment == null || string.IsNullOrWhiteSpace(command)) return;
            switch (command.Trim().ToLowerInvariant())
            {
                case "start": experiment.StartMeasurement(); break;
                case "stop": experiment.StopAndRecord(); break;
                case "reset": experiment.ResetExperiment(); break;
                case "clear": experiment.ClearResults(); break;
                case "length_plus": experiment.SetLength(experiment.LengthMetres + lengthStep); break;
                case "length_minus": experiment.SetLength(experiment.LengthMetres - lengthStep); break;
            }
        }
    }
}
