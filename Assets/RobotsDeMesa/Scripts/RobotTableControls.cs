using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace RobotsDeMesa
{
    public sealed class RobotTableControls : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager planes;
        [SerializeField] private ObjectSpawner spawner;
        [SerializeField] private ARInteractorSpawnTrigger placement;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text instructions;
        [SerializeField] private Button waveButton;
        [SerializeField] private Button removeButton;

        private RobotCompanion robot;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            spawner.objectSpawned += OnRobotPlaced;
            waveButton.onClick.AddListener(Wave);
            removeButton.onClick.AddListener(Remove);
            Update();
        }

        private void OnDisable()
        {
            spawner.objectSpawned -= OnRobotPlaced;
            waveButton.onClick.RemoveListener(Wave);
            removeButton.onClick.RemoveListener(Remove);
        }

        private void Update()
        {
            UpdateSafeArea();

            bool hasRobot = robot != null;
            placement.enabled = !hasRobot;
            waveButton.interactable = hasRobot;
            removeButton.interactable = hasRobot;

            if (ARSession.state == ARSessionState.Unsupported)
                ShowStatus("RA NO DISPONIBLE", "Este dispositivo no puede iniciar una sesión de realidad aumentada.");
            else if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Installing)
                ShowStatus("PREPARANDO ARCORE", "Completá la instalación de Servicios de Google Play para RA.");
            else if (ARSession.state != ARSessionState.SessionTracking)
                ShowStatus("BUSCANDO SEGUIMIENTO", "Apuntá a una mesa bien iluminada y mové el celular despacio.");
            else if (hasRobot)
                ShowStatus("MILO EN MESA", "Tocá el robot para seleccionarlo. Arrastrá para moverlo o girá con dos dedos.");
            else if (HasTrackedSurface())
                ShowStatus("SUPERFICIE DETECTADA", "Tocá la zona marcada para colocar a Milo.");
            else
                ShowStatus("BUSCANDO UNA MESA", "Apuntá a una superficie horizontal y mové el celular despacio.");
        }

        private bool HasTrackedSurface()
        {
            foreach (ARPlane plane in planes.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp &&
                    plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null)
                    return true;
            }

            return false;
        }

        private void OnRobotPlaced(GameObject spawned)
        {
            if (robot != null)
            {
                Destroy(spawned);
                return;
            }

            robot = spawned.GetComponent<RobotCompanion>();
            Update();
        }

        private void Wave()
        {
            if (robot != null)
                robot.Wave();
        }

        private void Remove()
        {
            if (robot == null)
                return;

            Destroy(robot.gameObject);
        }

        private void ShowStatus(string title, string detail)
        {
            if (status.text == title)
                return;

            status.text = title;
            instructions.text = detail;
        }

        private void UpdateSafeArea()
        {
            Rect area = Screen.safeArea;
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            if (size.x == 0 || size.y == 0 || (size == lastScreenSize && area == lastSafeArea))
                return;

            lastSafeArea = area;
            lastScreenSize = size;
            safeArea.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeArea.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        }
    }
}
