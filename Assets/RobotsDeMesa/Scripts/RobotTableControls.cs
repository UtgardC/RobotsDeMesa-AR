using TMPro;
using Unity.Collections;
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
        [SerializeField] private Button moveButton;
        [SerializeField] private Button sitButton;
        [SerializeField] private TMP_Text sitLabel;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button actionsToggle;
        [SerializeField] private Button toolsToggle;
        [SerializeField] private GameObject actionsMenu;
        [SerializeField] private GameObject toolsMenu;
        [SerializeField] private Button cubeButton;
        [SerializeField] private Button ballButton;
        [SerializeField] private Button blockButton;
        [SerializeField] private TableToolbox toolbox;

        private RobotCompanion robot;
        private int blockPlacementThroughFrame = -1;
        private float movementHintUntil;
        private float propHintUntil;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            spawner.objectSpawned += OnRobotPlaced;
            placement.objectSpawnTriggered.AddListener(Place);
            actionsToggle.onClick.AddListener(ToggleActions);
            toolsToggle.onClick.AddListener(ToggleTools);
            cubeButton.onClick.AddListener(SelectCube);
            ballButton.onClick.AddListener(SelectBall);
            blockButton.onClick.AddListener(SelectBlock);
            waveButton.onClick.AddListener(Wave);
            moveButton.onClick.AddListener(Move);
            sitButton.onClick.AddListener(ToggleSit);
            removeButton.onClick.AddListener(Remove);
            Update();
        }

        private void OnDisable()
        {
            spawner.objectSpawned -= OnRobotPlaced;
            placement.objectSpawnTriggered.RemoveListener(Place);
            actionsToggle.onClick.RemoveListener(ToggleActions);
            toolsToggle.onClick.RemoveListener(ToggleTools);
            cubeButton.onClick.RemoveListener(SelectCube);
            ballButton.onClick.RemoveListener(SelectBall);
            blockButton.onClick.RemoveListener(SelectBlock);
            waveButton.onClick.RemoveListener(Wave);
            moveButton.onClick.RemoveListener(Move);
            sitButton.onClick.RemoveListener(ToggleSit);
            removeButton.onClick.RemoveListener(Remove);
        }

        private void Update()
        {
            UpdateSafeArea();

            bool hasRobot = robot != null;
            bool tracking = ARSession.state == ARSessionState.SessionTracking;
            bool canInteract = hasRobot && tracking && !robot.IsMoving && !robot.IsManipulated;
            placement.enabled = !actionsMenu.activeSelf && !toolsMenu.activeSelf && (!hasRobot || toolbox.HasSelection);
            actionsToggle.interactable = hasRobot;
            waveButton.interactable = canInteract;
            moveButton.interactable = canInteract;
            sitButton.interactable = canInteract;
            removeButton.interactable = hasRobot;

            string sitAction = hasRobot && robot.IsSeated ? "Levantarse" : "Sentarse";
            if (sitLabel.text != sitAction)
                sitLabel.text = sitAction;

            if (ARSession.state == ARSessionState.Unsupported)
                ShowStatus("RA NO DISPONIBLE", "Este dispositivo no puede iniciar la realidad aumentada.");
            else if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Installing)
                ShowStatus("PREPARANDO ARCORE", "Completá la instalación de Servicios de Google Play para RA.");
            else if (!tracking)
                ShowStatus("BUSCANDO SEGUIMIENTO", "Apuntá a una mesa iluminada y mové el celular despacio.");
            else if (toolsMenu.activeSelf)
                ShowStatus("CAJA DE HERRAMIENTAS", "Elegí una pieza para colocar.");
            else if (toolbox.HasSelection)
                ShowStatus("COLOCAR " + toolbox.SelectedLabel.ToUpperInvariant(), "Tocá el plano. Tocá la caja para cancelar.");
            else if (Time.unscaledTime < propHintUntil)
                ShowStatus("PIEZA COLOCADA", "Tocá y arrastrá la pieza para moverla.");
            else if (hasRobot)
                ShowRobotStatus();
            else if (HasTrackedSurface())
                ShowStatus("MESA LISTA", "Tocá la zona marcada para colocar a Milo.");
            else
                ShowStatus("BUSCANDO UNA MESA", "Mové el celular despacio sobre una superficie horizontal.");
        }

        private void ShowRobotStatus()
        {
            if (robot.IsMoving)
                ShowStatus("EN MOVIMIENTO", "Milo está dando unos pasos.");
            else if (Time.unscaledTime < movementHintUntil)
                ShowStatus("POCO ESPACIO", "Acercá a Milo al centro de la mesa o escaneá más superficie.");
            else if (robot.IsSeated)
                ShowStatus("DESCANSANDO", "Puede saludar sentado o levantarse para caminar.");
            else
                ShowStatus("LISTO PARA JUGAR", "Elegí una acción o arrastrá a Milo. Giralo con dos dedos.");
        }

        private bool HasTrackedSurface()
        {
            foreach (ARPlane plane in planes.trackables)
            {
                if (IsTrackedTable(plane))
                    return true;
            }

            return false;
        }

        private static bool IsTrackedTable(ARPlane plane)
        {
            return plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null;
        }

        private void ToggleActions()
        {
            if (robot == null)
                return;

            bool open = !actionsMenu.activeSelf;
            CloseMenus();
            toolbox.Cancel();
            actionsMenu.SetActive(open);
            Update();
        }

        private void ToggleTools()
        {
            bool open = !toolsMenu.activeSelf;
            CloseMenus();
            toolbox.Cancel();
            toolsMenu.SetActive(open);
            Update();
        }

        private void SelectCube() => SelectTool(0);
        private void SelectBall() => SelectTool(1);
        private void SelectBlock() => SelectTool(2);

        private void SelectTool(int index)
        {
            CloseMenus();
            toolbox.Select(index);
            propHintUntil = 0f;
            Update();
        }

        private void CloseMenus()
        {
            actionsMenu.SetActive(false);
            toolsMenu.SetActive(false);
            // XRI processes placement after the UI click, even if that click hid a button.
            blockPlacementThroughFrame = Time.frameCount + 1;
        }

        private void Place(Vector3 position, Vector3 normal)
        {
            if (!placement.enabled || Time.frameCount <= blockPlacementThroughFrame ||
                ARSession.state != ARSessionState.SessionTracking)
                return;

            if (toolbox.HasSelection)
            {
                if (toolbox.TryPlace(position, normal))
                    propHintUntil = Time.unscaledTime + 2.5f;
                Update();
            }
            else if (robot == null)
            {
                spawner.TrySpawnObject(position, normal);
            }
        }

        private void OnRobotPlaced(GameObject spawned)
        {
            if (robot != null)
            {
                Destroy(spawned);
                return;
            }

            robot = spawned.GetComponent<RobotCompanion>();
            movementHintUntil = 0;
            propHintUntil = 0;
            Update();
        }

        private void Wave()
        {
            CloseMenus();
            propHintUntil = 0;
            if (robot != null)
                robot.Wave();
        }

        private void Move()
        {
            CloseMenus();
            propHintUntil = 0;
            if (robot == null || robot.IsMoving || robot.IsManipulated ||
                ARSession.state != ARSessionState.SessionTracking)
                return;

            movementHintUntil = 0;
            if (!TryFindDestination(out Vector3 destination))
                movementHintUntil = Time.unscaledTime + 3f;
            else
                robot.TryMove(destination);

            Update();
        }

        private bool TryFindDestination(out Vector3 destination)
        {
            destination = default;
            float closestHeight = .08f;
            ARPlane table = null;
            Vector2 origin = default;

            foreach (ARPlane plane in planes.trackables)
            {
                if (!IsTrackedTable(plane))
                    continue;

                Vector3 local = plane.transform.InverseTransformPoint(robot.transform.position);
                float height = Mathf.Abs(local.y) * Mathf.Abs(plane.transform.lossyScale.y);
                NativeArray<Vector2> boundary = plane.boundary;
                Vector2 point = new Vector2(local.x, local.z);
                if (height >= closestHeight || !boundary.IsCreated || boundary.Length < 3 ||
                    !ContainsPoint(boundary, point, 0f))
                    continue;

                closestHeight = height;
                table = plane;
                origin = point;
            }

            if (table == null)
                return false;

            Vector3 forward = table.transform.InverseTransformDirection(robot.transform.forward);
            Vector2 heading = new Vector2(forward.x, forward.z).normalized;
            float planeScale = Mathf.Abs(table.transform.lossyScale.x);
            if (heading == Vector2.zero || planeScale < .001f)
                return false;

            Vector3 scale = robot.transform.lossyScale;
            float robotScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float step = .2f * Mathf.Clamp(robotScale, .65f, 1.3f) / planeScale;
            // The collider's half diagonal allows Milo to turn without crossing the edge.
            float clearance = .103f * robotScale / planeScale;
            if (!TryStepOnPlane(table.boundary, origin, heading, step, clearance, out Vector2 target))
                return false;

            destination = table.transform.TransformPoint(new Vector3(target.x, 0f, target.y));
            return true;
        }

        private static bool TryStepOnPlane(NativeArray<Vector2> boundary, Vector2 origin, Vector2 heading,
            float step, float clearance, out Vector2 target)
        {
            for (int length = 0; length < 2; length++)
            {
                float distance = length == 0 ? step : step * .6f;
                Vector2 direction = heading;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = direction * distance;
                    Vector2 candidate = origin + offset;
                    if (ContainsPoint(boundary, candidate, clearance) &&
                        ContainsPoint(boundary, origin + offset * .5f, 0f))
                    {
                        target = candidate;
                        return true;
                    }

                    direction = (direction + Vector2.Perpendicular(direction)) * .70710678f;
                }
            }

            target = default;
            return false;
        }

        private static bool ContainsPoint(NativeArray<Vector2> boundary, Vector2 point, float clearance)
        {
            bool inside = false;
            float clearanceSquared = clearance * clearance;
            for (int i = 0, previous = boundary.Length - 1; i < boundary.Length; previous = i++)
            {
                Vector2 a = boundary[previous];
                Vector2 b = boundary[i];
                Vector2 edge = b - a;
                if (clearance > 0f && edge.sqrMagnitude > 0f)
                {
                    float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                    if ((point - (a + t * edge)).sqrMagnitude < clearanceSquared)
                        return false;
                }

                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < a.x + (point.y - a.y) * edge.x / edge.y)
                    inside = !inside;
            }

            return inside;
        }

        private void ToggleSit()
        {
            CloseMenus();
            propHintUntil = 0;
            if (robot != null)
            {
                movementHintUntil = 0;
                robot.ToggleSit();
                Update();
            }
        }

        private void Remove()
        {
            CloseMenus();
            propHintUntil = 0;
            if (robot != null)
                Destroy(robot.gameObject);
        }

        private void ShowStatus(string title, string detail)
        {
            if (status.text != title)
                status.text = title;
            if (instructions.text != detail)
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
