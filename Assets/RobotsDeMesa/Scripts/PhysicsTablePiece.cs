using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace RobotsDeMesa
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class PhysicsTablePiece : XRBaseGrabTransformer
    {
        private const float PickupLift = .13f;
        private const float LiftTime = .22f;

        private XRGrabInteractable grab;
        private Plane dragPlane;
        private Vector3 grabPosition;
        private Vector3 startOnPlane;
        private float pickupTime;
        private float spawnHeight;
        private bool dragging;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            spawnHeight = transform.position.y;
        }

        private void LateUpdate()
        {
            if (!grab.isSelected && transform.position.y < spawnHeight - 1.35f)
                Destroy(gameObject);
        }

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            dragging = false;
            grabPosition = grabInteractable.transform.position;
            pickupTime = Time.time;
        }

        public override void Process(XRGrabInteractable grabInteractable,
            XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            var interactor = grabInteractable.GetOldestInteractorSelecting();
            if (interactor == null)
                return;

            Transform origin = interactor is XRRayInteractor rayInteractor
                ? rayInteractor.rayOriginTransform
                : null;
            if (origin == null)
                origin = interactor.GetAttachTransform(grabInteractable);

            var ray = new Ray(origin.position, origin.forward);
            if (!dragging)
            {
                Camera camera = Camera.main;
                dragPlane = new Plane(camera != null ? camera.transform.forward : ray.direction, grabPosition);
                if (!dragPlane.Raycast(ray, out float initialDistance))
                    return;

                startOnPlane = ray.GetPoint(initialDistance);
                dragging = true;
            }

            if (!dragPlane.Raycast(ray, out float distance) || distance > 5f)
                return;

            // Drive the rigidbody toward the touch ray at its original depth.
            // The grab interactable estimates release velocity from this changing target.
            Vector3 target = grabPosition + ray.GetPoint(distance) - startOnPlane;
            target.y += Mathf.SmoothStep(0f, PickupLift, (Time.time - pickupTime) / LiftTime);
            target.y = Mathf.Max(spawnHeight, target.y);
            targetPose.position = target;
        }
    }
}
