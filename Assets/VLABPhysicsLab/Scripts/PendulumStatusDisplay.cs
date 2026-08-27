using TMPro;
using UnityEngine;

namespace VLAB.PhysicsLab
{
    /// <summary>World-space board that keeps the learner informed without leaving VR.</summary>
    public class PendulumStatusDisplay : MonoBehaviour
    {
        [SerializeField] private PendulumExperiment experiment;
        [SerializeField] private TMP_Text text;

        public void Configure(PendulumExperiment target, TMP_Text targetText)
        {
            experiment = target;
            text = targetText;
        }

        private void Update()
        {
            if (experiment == null || text == null) return;
            string state = experiment.IsRunning
                ? $"ĐANG ĐO  {experiment.ElapsedSeconds:F2} s   {experiment.CompletedOscillations}/{experiment.TargetOscillations} dao động"
                : "Sẵn sàng: chọn L, sau đó bấm BẮT ĐẦU";
            string result = experiment.Observations.Count == 0
                ? "Chưa ghi kết quả."
                : $"Lần gần nhất: g = {experiment.Observations[experiment.Observations.Count - 1].gravityEstimate:F2} m/s²";
            text.text = "VLAB | CON LẮC ĐƠN\n" +
                        $"L = {experiment.LengthMetres:F2} m | N = {experiment.TargetOscillations}\n" +
                        state + "\n" + result +
                        "\nCông thức: g = 4π²L / T²";
        }
    }
}
