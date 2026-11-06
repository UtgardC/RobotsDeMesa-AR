using System.Collections;
using UnityEngine;

namespace RobotsDeMesa
{
    public sealed class RobotCompanion : MonoBehaviour
    {
        private const float LiftDuration = 0.24f;
        private const float WaveDuration = 0.72f;
        private const float LowerDuration = 0.29f;

        [SerializeField] private Transform armPivot;

        private Quaternion armRestRotation;
        private Coroutine activeWave;

        public void Configure(Transform pivot)
        {
            armPivot = pivot;
            armRestRotation = pivot.localRotation;
        }

        private void Awake()
        {
            if (armPivot != null)
                armRestRotation = armPivot.localRotation;
        }

        private void OnDisable()
        {
            if (activeWave != null)
            {
                StopCoroutine(activeWave);
                activeWave = null;
            }

            if (armPivot != null)
                armPivot.localRotation = armRestRotation;
        }

        public void Wave()
        {
            if (isActiveAndEnabled && armPivot != null && activeWave == null)
                activeWave = StartCoroutine(AnimateWave());
        }

        private IEnumerator AnimateWave()
        {
            const float duration = LiftDuration + WaveDuration + LowerDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float lift = Mathf.SmoothStep(0f, 1f, elapsed / LiftDuration);
                float lower = Mathf.SmoothStep(0f, 1f, (duration - elapsed) / LowerDuration);
                float waveProgress = Mathf.Clamp01((elapsed - LiftDuration) / WaveDuration);
                float swing = Mathf.Sin(waveProgress * 4f * Mathf.PI) * 18f;
                armPivot.localRotation = armRestRotation
                    * Quaternion.Euler(0f, 0f, (125f + swing) * lift * lower);

                yield return null;
                elapsed += Time.deltaTime;
            }

            armPivot.localRotation = armRestRotation;
            activeWave = null;
        }
    }
}
