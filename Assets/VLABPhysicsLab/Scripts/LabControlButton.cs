using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VLAB.PhysicsLab
{
    /// <summary>Physical XR control used on the experiment console.</summary>
    [RequireComponent(typeof(Collider))]
    public class LabControlButton : MonoBehaviour
    {
        public enum Command { ShorterString, LongerString, Start, StopAndRecord, Reset, ClearResults }

        [SerializeField] private PendulumExperiment experiment;
        [SerializeField] private Command command;
        [SerializeField] private float lengthStep = 0.10f;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

        public void Configure(PendulumExperiment target, Command newCommand)
        {
            experiment = target;
            command = newCommand;
        }

        private void Awake()
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable == null) interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
        }

        private void OnDestroy()
        {
            if (interactable != null) interactable.selectEntered.RemoveListener(OnSelected);
        }

        private void OnSelected(SelectEnterEventArgs _)
        {
            Execute();
        }

        // Lets teachers test the exact same console with a mouse in the Unity Editor.
        private void OnMouseDown()
        {
            Execute();
        }

        private void Execute()
        {
            if (experiment == null) return;
            switch (command)
            {
                case Command.ShorterString: experiment.SetLength(experiment.LengthMetres - lengthStep); break;
                case Command.LongerString: experiment.SetLength(experiment.LengthMetres + lengthStep); break;
                case Command.Start: experiment.StartMeasurement(); break;
                case Command.StopAndRecord: experiment.StopAndRecord(); break;
                case Command.Reset: experiment.ResetExperiment(); break;
                case Command.ClearResults: experiment.ClearResults(); break;
            }
        }
    }
}
