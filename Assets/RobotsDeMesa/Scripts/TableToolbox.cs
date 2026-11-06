using UnityEngine;

namespace RobotsDeMesa
{
    public sealed class TableToolbox : MonoBehaviour
    {
        private static readonly string[] PropLabels = { "Cubo", "Pelota", "Rectángulo" };

        [SerializeField] private GameObject[] propPrefabs;

        private int selection = -1;

        public bool HasSelection => selection >= 0;
        public int SelectedIndex => selection;
        public string SelectedLabel => HasSelection ? PropLabels[selection] : string.Empty;

        public void Select(int index)
        {
            if (index >= 0 && index < PropLabels.Length &&
                propPrefabs != null && index < propPrefabs.Length && propPrefabs[index] != null)
                selection = index;
        }

        public void Cancel()
        {
            selection = -1;
        }

        public void Clear()
        {
            Cancel();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject piece = transform.GetChild(i).gameObject;
                if (!piece.TryGetComponent<PhysicsTablePiece>(out _))
                    continue;

                piece.SetActive(false);
                Destroy(piece);
            }
        }

        public bool TryPlace(Vector3 position, Vector3 normal)
        {
            Vector3 up = normal.normalized;
            if (!HasSelection || up.y < .9f)
                return false;

            // Give the dynamic collider a little space above the detected plane to settle.
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up);
            Instantiate(propPrefabs[selection], position + up * .025f, rotation, transform);
            Cancel();
            return true;
        }
    }
}
