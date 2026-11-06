using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace RobotsDeMesa
{
    public sealed class RobotCollection : MonoBehaviour
    {
        public const int Count = 3;

        private static readonly string[] Names = { "Milo", "Nova", "Byte" };
        private static readonly string[] SaveKeys = { "", "robots-de-mesa.nova", "robots-de-mesa.byte" };

        [SerializeField] private ObjectSpawner spawner;
        [SerializeField] private ARTrackedImageManager trackedImages;
        [SerializeField] private GameObject[] cardPreviews;

        private readonly RobotCompanion[] placed = new RobotCompanion[Count];
        private readonly bool[] unlocked = new bool[Count];
        private readonly Dictionary<TrackableId, GameObject> previews = new();
        private int placingIndex = -1;

        public event Action<int> Unlocked;

        private void Awake()
        {
            unlocked[0] = true;
            for (int i = 1; i < Count; i++)
                unlocked[i] = PlayerPrefs.GetInt(SaveKeys[i], 0) != 0;
        }

        private void OnEnable()
        {
            spawner.objectSpawned += OnSpawned;
            trackedImages.trackablesChanged.AddListener(OnImagesChanged);

            foreach (ARTrackedImage image in trackedImages.trackables)
                UpdateImage(image);
        }

        private void OnDisable()
        {
            spawner.objectSpawned -= OnSpawned;
            trackedImages.trackablesChanged.RemoveListener(OnImagesChanged);
            foreach (GameObject preview in previews.Values)
            {
                if (preview != null)
                    Destroy(preview);
            }

            previews.Clear();
        }

        public bool IsUnlocked(int index) => IsValid(index) && unlocked[index];
        public string GetName(int index) => IsValid(index) ? Names[index] : string.Empty;
        public RobotCompanion GetRobot(int index) => IsValid(index) ? placed[index] : null;

        public int GetIndex(RobotCompanion robot)
        {
            if (robot == null)
                return -1;

            for (int i = 0; i < Count; i++)
            {
                if (placed[i] == robot)
                    return i;
            }

            return -1;
        }

        public bool TryPlace(int index, Vector3 position, Vector3 normal)
        {
            if (!IsUnlocked(index) || placed[index] != null || placingIndex >= 0 ||
                index >= spawner.objectPrefabs.Count || spawner.objectPrefabs[index] == null)
                return false;

            int previousIndex = spawner.spawnOptionIndex;
            placingIndex = index;
            spawner.spawnOptionIndex = index;

            try
            {
                return spawner.TrySpawnObject(position, normal) && placed[index] != null;
            }
            finally
            {
                spawner.spawnOptionIndex = previousIndex;
                placingIndex = -1;
            }
        }

        private void OnSpawned(GameObject instance)
        {
            if (placingIndex >= 0)
                placed[placingIndex] = instance.GetComponent<RobotCompanion>();
        }

        private void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> change)
        {
            foreach (ARTrackedImage image in change.added)
                UpdateImage(image);
            foreach (ARTrackedImage image in change.updated)
                UpdateImage(image);
            foreach (var pair in change.removed)
                RemovePreview(pair.Key);
        }

        private void UpdateImage(ARTrackedImage image)
        {
            int index = Array.IndexOf(Names, image.referenceImage.name);
            if (index <= 0 || image.trackingState != TrackingState.Tracking)
            {
                RemovePreview(image.trackableId);
                return;
            }

            if (!unlocked[index])
            {
                unlocked[index] = true;
                PlayerPrefs.SetInt(SaveKeys[index], 1);
                PlayerPrefs.Save();
                Unlocked?.Invoke(index);
            }

            if (!previews.ContainsKey(image.trackableId) && cardPreviews[index - 1] != null)
            {
                GameObject preview = Instantiate(cardPreviews[index - 1], image.transform, false);
                previews.Add(image.trackableId, preview);
            }
        }

        private void RemovePreview(TrackableId trackableId)
        {
            if (previews.Remove(trackableId, out GameObject preview) && preview != null)
                Destroy(preview);
        }

        private static bool IsValid(int index) => index >= 0 && index < Count;
    }
}
