using UnityEngine;

namespace RobotsDeMesa
{
    public sealed class TableToolbox : MonoBehaviour
    {
        private static readonly string[] PropLabels = { "Cubo", "Pelota", "Rectángulo" };

        [SerializeField] private GameObject[] propPrefabs;

        private int selection = -1;

        public bool HasSelection => selection >= 0;
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

        public bool TryPlace(Vector3 position, Vector3 normal)
        {
            if (!HasSelection || normal.normalized.y < .9f)
                return false;

            // Align the prefab's base pivot with the surface provided by the AR raycast.
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Instantiate(propPrefabs[selection], position, rotation, transform);
            Cancel();
            return true;
        }
    }
}
