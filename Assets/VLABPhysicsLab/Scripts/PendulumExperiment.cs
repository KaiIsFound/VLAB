using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLAB.PhysicsLab
{
    /// <summary>
    /// Learning model for the simple-pendulum experiment. Attach it to the pendulum pivot.
    /// It uses the small-angle model T = 2*pi*sqrt(L/g), records observations and exposes
    /// the calculated gravitational acceleration for a UI, XR button or ESP32 controller.
    /// </summary>
    public class PendulumExperiment : MonoBehaviour
    {
        [Serializable]
        public struct Observation
        {
            public float lengthMetres;
            public int oscillations;
            public float elapsedSeconds;
            public float periodSeconds;
            public float gravityEstimate;
        }

        [Header("Pendulum")]
        [SerializeField, Min(0.10f)] private float lengthMetres = 0.80f;
        [SerializeField, Range(2f, 15f)] private float releaseAngleDegrees = 8f;
        [SerializeField] private Transform bob;
        [SerializeField] private Transform stringVisual;
        [SerializeField] private bool animateBob = true;

        [Header("Measurement")]
        [SerializeField, Min(1)] private int targetOscillations = 10;
        [SerializeField] private float referenceGravity = 9.81f;
        [SerializeField, Range(0f, 0.08f)] private float measurementNoise = 0.012f;

        public bool IsRunning { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CompletedOscillations { get; private set; }
        public float LengthMetres => lengthMetres;
        public int TargetOscillations => targetOscillations;
        public IReadOnlyList<Observation> Observations => observations;
        public event Action<Observation> ObservationRecorded;
        public event Action ExperimentCompleted;

        private readonly List<Observation> observations = new List<Observation>();
        private float phase;
        private float lastCycle;

        private void Start() => ResetExperiment();

        /// <summary>Called by the editor lab builder after it creates the pendulum geometry.</summary>
        public void ConfigureVisuals(Transform bobTransform, Transform stringTransform)
        {
            bob = bobTransform;
            stringVisual = stringTransform;
            Rigidbody bobBody = bob != null ? bob.GetComponent<Rigidbody>() : null;
            if (bobBody != null) bobBody.isKinematic = true;
            ResetExperiment();
        }

        private void Update()
        {
            if (!IsRunning) return;

            ElapsedSeconds += Time.deltaTime;
            float angularFrequency = Mathf.Sqrt(referenceGravity / lengthMetres);
            phase += angularFrequency * Time.deltaTime;
            float cycles = phase / (2f * Mathf.PI);
            CompletedOscillations = Mathf.Min(targetOscillations, Mathf.FloorToInt(cycles));

            if (animateBob && bob != null)
            {
                float angle = releaseAngleDegrees * Mathf.Sin(phase);
                bob.localPosition = new Vector3(
                    lengthMetres * Mathf.Sin(angle * Mathf.Deg2Rad),
                    -lengthMetres * Mathf.Cos(angle * Mathf.Deg2Rad), 0f);
                bob.localRotation = Quaternion.identity;
                if (stringVisual != null)
                {
                    stringVisual.localScale = new Vector3(stringVisual.localScale.x, lengthMetres * 0.5f, stringVisual.localScale.z);
                    stringVisual.localPosition = new Vector3(
                        lengthMetres * 0.5f * Mathf.Sin(angle * Mathf.Deg2Rad),
                        -lengthMetres * 0.5f * Mathf.Cos(angle * Mathf.Deg2Rad), 0f);
                    stringVisual.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
            }

            if (cycles >= targetOscillations && lastCycle < targetOscillations)
            {
                StopAndRecord();
            }
            lastCycle = cycles;
        }

        public void SetLength(float metres)
        {
            lengthMetres = Mathf.Clamp(metres, 0.10f, 2.00f);
            ResetExperiment();
        }

        public void SetOscillations(float count)
        {
            targetOscillations = Mathf.Clamp(Mathf.RoundToInt(count), 1, 50);
            ResetExperiment();
        }

        public void StartMeasurement()
        {
            if (IsRunning) return;
            ElapsedSeconds = 0f;
            CompletedOscillations = 0;
            phase = 0f;
            lastCycle = 0f;
            IsRunning = true;
        }

        public void StopAndRecord()
        {
            if (!IsRunning) return;
            IsRunning = false;
            float noisyTime = ElapsedSeconds * (1f + UnityEngine.Random.Range(-measurementNoise, measurementNoise));
            float period = noisyTime / targetOscillations;
            Observation observation = new Observation
            {
                lengthMetres = lengthMetres,
                oscillations = targetOscillations,
                elapsedSeconds = noisyTime,
                periodSeconds = period,
                gravityEstimate = 4f * Mathf.PI * Mathf.PI * lengthMetres / (period * period)
            };
            observations.Add(observation);
            ObservationRecorded?.Invoke(observation);
            ExperimentCompleted?.Invoke();
        }

        public void ResetExperiment()
        {
            IsRunning = false;
            ElapsedSeconds = 0f;
            CompletedOscillations = 0;
            phase = 0f;
            lastCycle = 0f;
            if (bob != null) bob.localPosition = Vector3.down * lengthMetres;
            if (stringVisual != null)
            {
                stringVisual.localPosition = Vector3.down * (lengthMetres * 0.5f);
                stringVisual.localScale = new Vector3(stringVisual.localScale.x, lengthMetres * 0.5f, stringVisual.localScale.z);
            }
        }

        public void ClearResults() => observations.Clear();
    }
}
