using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace RobotsDeMesa
{
    public sealed class RobotTableControls : MonoBehaviour
    {
        private static readonly Color32[] RobotColors =
        {
            new(126, 245, 205, 255), new(255, 193, 113, 255), new(179, 241, 111, 255)
        };

        private static readonly Color32[] ToolColors =
        {
            new(114, 201, 255, 255), new(255, 165, 96, 255), new(199, 166, 255, 255)
        };

        private static readonly string[] ToolNames = { "un cubo", "una pelota", "un bloque rectangular" };

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
        [SerializeField] private Button clearButton;
        [SerializeField] private TableToolbox toolbox;
        [SerializeField] private RobotCollection collection;
        [SerializeField] private Button[] robotTabs;
        [SerializeField] private GameObject[] tabLocks;
        [SerializeField] private Image[] tabHighlights;
        [SerializeField] private TMP_Text actionsLabel;
        [SerializeField] private Image actionsAccent;
        [SerializeField] private Image tooltipAccent;

        private int selectedIndex;
        private int lockedIndex = -1;
        private int blockPlacementThroughFrame = -1;
        private float noticeUntil;
        private string noticeTitle;
        private string noticeDetail;
        private Color tooltipDefault;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private RobotCompanion SelectedRobot => collection.GetRobot(selectedIndex);

        private void Awake()
        {
            if (tooltipAccent != null)
                tooltipDefault = tooltipAccent.color;
        }

        private void OnEnable()
        {
            spawner.objectSpawned += OnRobotPlaced;
            collection.Unlocked += OnRobotUnlocked;
            for (int i = 0; i < robotTabs.Length; i++)
            {
                var robot = collection.GetRobot(i);
                if (robot != null)
                    OnRobotPlaced(robot.gameObject);
            }

            placement.objectSpawnTriggered.AddListener(Place);
            robotTabs[0].onClick.AddListener(SelectMilo);
            robotTabs[1].onClick.AddListener(SelectNova);
            robotTabs[2].onClick.AddListener(SelectByte);
            actionsToggle.onClick.AddListener(ToggleActions);
            toolsToggle.onClick.AddListener(ToggleTools);
            cubeButton.onClick.AddListener(SelectCube);
            ballButton.onClick.AddListener(SelectBall);
            blockButton.onClick.AddListener(SelectBlock);
            clearButton.onClick.AddListener(ClearTools);
            waveButton.onClick.AddListener(Wave);
            moveButton.onClick.AddListener(Move);
            sitButton.onClick.AddListener(ToggleSit);
            removeButton.onClick.AddListener(Remove);
            Update();
        }

        private void OnDisable()
        {
            spawner.objectSpawned -= OnRobotPlaced;
            collection.Unlocked -= OnRobotUnlocked;
            for (int i = 0; i < robotTabs.Length; i++)
            {
                var robot = collection.GetRobot(i);
                if (robot != null && robot.TryGetComponent<XRGrabInteractable>(out var grab))
                    grab.selectEntered.RemoveListener(OnRobotSelected);
            }

            placement.objectSpawnTriggered.RemoveListener(Place);
            robotTabs[0].onClick.RemoveListener(SelectMilo);
            robotTabs[1].onClick.RemoveListener(SelectNova);
            robotTabs[2].onClick.RemoveListener(SelectByte);
            actionsToggle.onClick.RemoveListener(ToggleActions);
            toolsToggle.onClick.RemoveListener(ToggleTools);
            cubeButton.onClick.RemoveListener(SelectCube);
            ballButton.onClick.RemoveListener(SelectBall);
            blockButton.onClick.RemoveListener(SelectBlock);
            clearButton.onClick.RemoveListener(ClearTools);
            waveButton.onClick.RemoveListener(Wave);
            moveButton.onClick.RemoveListener(Move);
            sitButton.onClick.RemoveListener(ToggleSit);
            removeButton.onClick.RemoveListener(Remove);
        }

        private void Update()
        {
            UpdateSafeArea();
            var robot = SelectedRobot;
            bool hasRobot = robot != null;
            bool tracking = ARSession.state == ARSessionState.SessionTracking;
            bool canInteract = hasRobot && tracking && !robot.IsManipulated;
            bool busy = hasRobot && (robot.IsMoving || robot.IsWandering);

            placement.enabled = !actionsMenu.activeSelf && !toolsMenu.activeSelf &&
                (toolbox.HasSelection || (lockedIndex < 0 && !hasRobot));
            actionsToggle.interactable = hasRobot;
            waveButton.interactable = canInteract && !busy;
            moveButton.interactable = canInteract && !busy;
            sitButton.interactable = canInteract && !busy;
            removeButton.interactable = hasRobot;

            string name = collection.GetName(selectedIndex);
            SetText(actionsLabel, name);
            SetText(sitLabel, hasRobot && robot.IsSeated ? "Levantarse" : "Sentarse");
            actionsAccent.color = RobotColors[selectedIndex];

            for (int i = 0; i < robotTabs.Length; i++)
            {
                if (tabLocks[i] != null)
                    tabLocks[i].SetActive(!collection.IsUnlocked(i));
                tabHighlights[i].enabled = i == (lockedIndex >= 0 ? lockedIndex : selectedIndex);
            }

            if (ARSession.state == ARSessionState.Unsupported)
                ShowStatus("RA NO DISPONIBLE", "Este dispositivo no puede iniciar la realidad aumentada.");
            else if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Installing)
                ShowStatus("PREPARANDO ARCORE", "Completá la instalación de Servicios de Google Play para RA.");
            else if (lockedIndex >= 0)
                ShowStatus(collection.GetName(lockedIndex).ToUpperInvariant() + " · BLOQUEADO",
                    "Colocá su tarjeta sobre la mesa y apuntá con la cámara para desbloquearlo.", RobotColors[lockedIndex]);
            else if (toolbox.HasSelection)
                ShowStatus("COLOCAR " + toolbox.SelectedLabel.ToUpperInvariant(),
                    "Tocá una superficie para crear " + ToolNames[toolbox.SelectedIndex] + ".", ToolColors[toolbox.SelectedIndex]);
            else if (!tracking)
                ShowStatus("BUSCANDO SEGUIMIENTO", "Apuntá a una mesa iluminada y mové el celular despacio.");
            else if (toolsMenu.activeSelf)
                ShowStatus("CAJA DE HERRAMIENTAS", "Elegí una pieza o limpiá las herramientas de la mesa.");
            else if (Time.unscaledTime < noticeUntil)
                ShowStatus(noticeTitle, noticeDetail);
            else if (busy)
                ShowStatus("EN MOVIMIENTO", name + " está explorando la mesa.");
            else if (hasRobot)
                ShowStatus(robot.IsSeated ? "DESCANSANDO" : "LISTO PARA JUGAR",
                    "Tocá un robot para elegirlo y abrí su menú de acciones.", RobotColors[selectedIndex]);
            else if (HasTrackedSurface())
                ShowStatus("MESA LISTA", "Tocá la zona marcada para colocar a " + name + ".", RobotColors[selectedIndex]);
            else
                ShowStatus("BUSCANDO UNA MESA", "Mové el celular despacio sobre una superficie horizontal.");
        }

        private bool HasTrackedSurface()
        {
            foreach (ARPlane plane in planes.trackables)
                if (IsTrackedTable(plane))
                    return true;
            return false;
        }

        private static bool IsTrackedTable(ARPlane plane)
        {
            return plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null;
        }

        private void SelectMilo() => SelectRobotTab(0);
        private void SelectNova() => SelectRobotTab(1);
        private void SelectByte() => SelectRobotTab(2);

        private void SelectRobotTab(int index)
        {
            bool open = collection.GetRobot(index) != null &&
                (selectedIndex != index || !actionsMenu.activeSelf);
            CloseMenus();
            toolbox.Cancel();
            noticeUntil = 0f;
            if (collection.IsUnlocked(index))
            {
                lockedIndex = -1;
                selectedIndex = index;
                actionsMenu.SetActive(open);
            }
            else
            {
                lockedIndex = index;
            }
            Update();
        }

        private void OnRobotUnlocked(int index)
        {
            if (lockedIndex == index)
            {
                selectedIndex = index;
                lockedIndex = -1;
            }
            ShowNotice("ROBOT DESBLOQUEADO", "Ya podés elegir a " + collection.GetName(index) + " en la colección.");
        }

        private void OnRobotPlaced(GameObject spawned)
        {
            if (spawned.TryGetComponent<XRGrabInteractable>(out var grab))
                grab.selectEntered.AddListener(OnRobotSelected);
        }

        private void OnRobotSelected(SelectEnterEventArgs args)
        {
            var robot = args.interactableObject.transform.GetComponent<RobotCompanion>();
            int index = collection.GetIndex(robot);
            if (index < 0)
                return;

            CloseMenus();
            toolbox.Cancel();
            lockedIndex = -1;
            selectedIndex = index;
            noticeUntil = 0f;
            Update();
        }

        private void ToggleActions()
        {
            if (SelectedRobot == null)
                return;

            bool open = !actionsMenu.activeSelf;
            CloseMenus();
            toolbox.Cancel();
            lockedIndex = -1;
            actionsMenu.SetActive(open);
            Update();
        }

        private void ToggleTools()
        {
            bool open = !toolsMenu.activeSelf;
            CloseMenus();
            toolbox.Cancel();
            lockedIndex = -1;
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
            noticeUntil = 0f;
            Update();
        }

        private void ClearTools()
        {
            CloseMenus();
            toolbox.Clear();
            ShowNotice("MESA ORDENADA", "Se retiraron todas las piezas de herramientas.");
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
                    ShowNotice("PIEZA COLOCADA", "Arrastrala para levantarla y soltá con impulso para lanzarla.");
            }
            else if (lockedIndex < 0 && collection.TryPlace(selectedIndex, position, normal))
            {
                noticeUntil = 0f;
            }
            Update();
        }

        private void Wave()
        {
            CloseMenus();
            noticeUntil = 0f;
            var robot = SelectedRobot;
            if (robot != null)
                robot.Wave();
        }

        private void Move()
        {
            CloseMenus();
            noticeUntil = 0f;
            var robot = SelectedRobot;
            if (robot != null && !robot.TryWander(() => FindWanderDestination(robot)))
                ShowNotice("POCO ESPACIO", "Acercá al robot al centro o escaneá más superficie.");
            Update();
        }

        private Vector3? FindWanderDestination(RobotCompanion robot)
        {
            if (ARSession.state != ARSessionState.SessionTracking)
                return null;
            return TryFindDestination(robot, out Vector3 destination) ? destination : null;
        }

        private bool TryFindDestination(RobotCompanion robot, out Vector3 destination)
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

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 heading = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float planeScale = Mathf.Abs(table.transform.lossyScale.x);
            if (planeScale < .001f)
                return false;

            Vector3 scale = robot.transform.lossyScale;
            float robotScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float step = Random.Range(.16f, .29f) * Mathf.Clamp(robotScale, .65f, 1.3f) / planeScale;
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
            noticeUntil = 0f;
            var robot = SelectedRobot;
            if (robot != null)
                robot.ToggleSit();
            Update();
        }

        private void Remove()
        {
            CloseMenus();
            noticeUntil = 0f;
            var robot = SelectedRobot;
            if (robot != null)
                Destroy(robot.gameObject);
        }

        private void ShowNotice(string title, string detail)
        {
            noticeTitle = title;
            noticeDetail = detail;
            noticeUntil = Time.unscaledTime + 3f;
        }

        private void ShowStatus(string title, string detail, Color? accent = null)
        {
            SetText(status, title);
            SetText(instructions, detail);
            status.color = accent ?? (Color)RobotColors[0];
            if (tooltipAccent != null)
                tooltipAccent.color = accent ?? tooltipDefault;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label.text != value)
                label.text = value;
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
