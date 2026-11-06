using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RobotsDeMesa
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class RobotCompanion : MonoBehaviour
    {
        private const float LiftDuration = 0.24f;
        private const float WaveDuration = 0.72f;
        private const float LowerDuration = 0.29f;
        private const float SitDuration = 0.3f;
        private const float WalkDuration = 0.78f;

        [SerializeField] private Transform armPivot;

        private Quaternion armRestRotation;
        private XRGrabInteractable grab;
        private Coroutine activeWave;
        private Coroutine bodyAction;
        private BodyPart[] body;
        private BodyPart leftLeg;
        private BodyPart leftFoot;
        private BodyPart leftToe;
        private BodyPart rightLeg;
        private BodyPart rightFoot;
        private BodyPart rightToe;
        private float seatedAmount;

        public bool IsSeated { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsManipulated => grab != null && grab.isSelected;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (armPivot != null)
                armRestRotation = armPivot.localRotation;

            Transform model = armPivot != null ? armPivot.parent : transform.Find("Robot model");
            if (model == null)
                return;

            body = new[]
            {
                new BodyPart(model.Find("Torso")),
                new BodyPart(model.Find("Chest panel")),
                new BodyPart(model.Find("Status light")),
                new BodyPart(model.Find("Waist")),
                new BodyPart(model.Find("Neck")),
                new BodyPart(model.Find("Head")),
                new BodyPart(model.Find("Left arm")),
                new BodyPart(armPivot)
            };
            leftLeg = new BodyPart(model.Find("Left leg"));
            leftFoot = new BodyPart(model.Find("Left foot"));
            leftToe = new BodyPart(model.Find("Left toe"));
            rightLeg = new BodyPart(model.Find("Right leg"));
            rightFoot = new BodyPart(model.Find("Right foot"));
            rightToe = new BodyPart(model.Find("Right toe"));
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            StopBodyAction();

            IsSeated = false;
            SetBodyPose(0f, Vector3.zero, 0f);

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

        public void ToggleSit()
        {
            if (!isActiveAndEnabled || body == null || IsMoving || IsManipulated)
                return;

            StopBodyAction();
            IsSeated = !IsSeated;
            float target = IsSeated ? 1f : 0f;
            if (!Mathf.Approximately(seatedAmount, target))
                bodyAction = StartCoroutine(AnimateSit(target));
        }

        public bool TryMove(Vector3 destination)
        {
            if (!isActiveAndEnabled || body == null || IsMoving || IsManipulated ||
                (destination - transform.position).sqrMagnitude < 0.0001f)
                return false;

            StopBodyAction();
            IsSeated = false;
            IsMoving = true;
            bodyAction = StartCoroutine(AnimateWalk(destination));
            return true;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (bodyAction == null)
                return;

            StopBodyAction();
            SetBodyPose(IsSeated ? 1f : 0f, Vector3.zero, 0f);
        }

        private void StopBodyAction()
        {
            if (bodyAction != null)
            {
                StopCoroutine(bodyAction);
                bodyAction = null;
            }

            IsMoving = false;
        }

        private IEnumerator AnimateSit(float target)
        {
            yield return BlendSeat(target);
            bodyAction = null;
        }

        private IEnumerator BlendSeat(float target)
        {
            float start = seatedAmount;
            for (float elapsed = 0f; elapsed < SitDuration; elapsed += Time.deltaTime)
            {
                SetBodyPose(Mathf.SmoothStep(start, target, elapsed / SitDuration), Vector3.zero, 0f);
                yield return null;
            }

            SetBodyPose(target, Vector3.zero, 0f);
        }

        private IEnumerator AnimateWalk(Vector3 destination)
        {
            if (seatedAmount > 0f)
                yield return BlendSeat(0f);

            Vector3 origin = transform.position;
            Vector3 direction = transform.InverseTransformDirection(destination - origin);
            direction.y = 0f;
            direction.Normalize();

            for (float elapsed = 0f; elapsed < WalkDuration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / WalkDuration;
                transform.position = Vector3.LerpUnclamped(origin, destination, Mathf.SmoothStep(0f, 1f, progress));
                float stride = Mathf.Sin(progress * 4f * Mathf.PI) * Mathf.Sin(progress * Mathf.PI);
                SetBodyPose(0f, direction, stride);
                yield return null;
            }

            transform.position = destination;
            SetBodyPose(0f, Vector3.zero, 0f);
            IsMoving = false;
            bodyAction = null;
        }

        private void SetBodyPose(float sit, Vector3 direction, float stride)
        {
            seatedAmount = sit;
            if (body == null)
                return;

            Vector3 bodyOffset = new Vector3(0f, -0.031f * sit + Mathf.Abs(stride) * 0.004f, -0.015f * sit);
            foreach (BodyPart part in body)
                part.Offset(bodyOffset);

            Vector3 seatLegOffset = new Vector3(0f, -0.012f * sit, 0.014f * sit);
            Vector3 seatFootOffset = Vector3.forward * (0.030f * sit);
            Vector3 strideOffset = direction * (stride * 0.011f);
            Vector3 leftStep = strideOffset + Vector3.up * (Mathf.Max(0f, stride) * 0.010f);
            Vector3 rightStep = -strideOffset + Vector3.up * (Mathf.Max(0f, -stride) * 0.010f);
            leftLeg.Offset(seatLegOffset + leftStep * 0.55f, -68f * sit);
            rightLeg.Offset(seatLegOffset + rightStep * 0.55f, -68f * sit);
            leftFoot.Offset(seatFootOffset + leftStep);
            leftToe.Offset(seatFootOffset + leftStep);
            rightFoot.Offset(seatFootOffset + rightStep);
            rightToe.Offset(seatFootOffset + rightStep);
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

        private readonly struct BodyPart
        {
            private readonly Transform part;
            private readonly Vector3 restPosition;
            private readonly Quaternion restRotation;

            public BodyPart(Transform part)
            {
                this.part = part;
                restPosition = part != null ? part.localPosition : Vector3.zero;
                restRotation = part != null ? part.localRotation : Quaternion.identity;
            }

            public void Offset(Vector3 offset)
            {
                if (part != null)
                    part.localPosition = restPosition + offset;
            }

            public void Offset(Vector3 offset, float pitch)
            {
                if (part != null)
                    part.SetLocalPositionAndRotation(restPosition + offset, restRotation * Quaternion.Euler(pitch, 0f, 0f));
            }
        }
    }
}
