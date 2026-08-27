using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VLAB.PhysicsLab
{
    /// <summary>UI adapter for PendulumExperiment. All references are intentionally set in the Inspector.</summary>
    public class PendulumExperimentPanel : MonoBehaviour
    {
        [SerializeField] private PendulumExperiment experiment;
        [SerializeField] private Slider lengthSlider;
        [SerializeField] private Slider oscillationSlider;
        [SerializeField] private TMP_Text lengthLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private TMP_Text resultsLabel;

        private void OnEnable()
        {
            if (experiment != null) experiment.ObservationRecorded += AddObservation;
            RefreshControls();
        }

        private void OnDisable()
        {
            if (experiment != null) experiment.ObservationRecorded -= AddObservation;
        }

        private void Update()
        {
            if (experiment == null) return;
            if (timerLabel != null)
                timerLabel.text = experiment.IsRunning
                    ? $"{experiment.ElapsedSeconds:F2} s  |  {experiment.CompletedOscillations}/{experiment.TargetOscillations} dao động"
                    : "Sẵn sàng đo";
        }

        public void ChangeLength(float metres) { experiment.SetLength(metres); RefreshControls(); }
        public void ChangeOscillations(float count) { experiment.SetOscillations(count); RefreshControls(); }
        public void StartMeasurement() { experiment.StartMeasurement(); instructionLabel.text = "Thả con lắc, quan sát và chờ đủ số dao động."; }
        public void StopMeasurement() { experiment.StopAndRecord(); }
        public void ResetMeasurement() { experiment.ResetExperiment(); instructionLabel.text = "Chọn chiều dài, sau đó bấm Bắt đầu đo."; }
        public void ClearResults()
        {
            experiment.ClearResults();
            if (resultsLabel != null) resultsLabel.text = "Chưa có số đo.";
        }

        private void RefreshControls()
        {
            if (experiment == null) return;
            if (lengthSlider != null) lengthSlider.SetValueWithoutNotify(experiment.LengthMetres);
            if (oscillationSlider != null) oscillationSlider.SetValueWithoutNotify(experiment.TargetOscillations);
            if (lengthLabel != null) lengthLabel.text = $"L = {experiment.LengthMetres:F2} m";
        }

        private void AddObservation(PendulumExperiment.Observation value)
        {
            StringBuilder rows = new StringBuilder();
            rows.AppendLine("L (m)    N    t (s)    T (s)    g (m/s²)");
            foreach (var item in experiment.Observations)
                rows.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,4:F2}    {1,2}   {2,5:F2}    {3,4:F2}      {4,4:F2}", item.lengthMetres, item.oscillations, item.elapsedSeconds, item.periodSeconds, item.gravityEstimate));
            if (resultsLabel != null) resultsLabel.text = rows.ToString();
            if (instructionLabel != null)
                instructionLabel.text = $"Đã ghi kết quả. g ≈ {value.gravityEstimate:F2} m/s². Thử một chiều dài khác để so sánh.";
        }
    }
}
