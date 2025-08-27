using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AddressableSystem
{
    /// <summary>
    /// Central manager for addressable assets, implemented as a singleton.
    /// </summary>
    public class AddressableDataCore : MonoBehaviour
    {
        private static AddressableDataCore instance;
        [SerializeField] private IAddressableDataContainer dataContainer = new AddressableDataContainer();
        private readonly Dictionary<int, Dictionary<GroupCategory, List<BaseAddressableData>>> sceneDataMap =
            new Dictionary<int, Dictionary<GroupCategory, List<BaseAddressableData>>>();
        private CancellationTokenSource cts = new CancellationTokenSource();

        public static AddressableDataCore Instance
        {
            get
            {
                if (instance == null)
                {
                    
                    var gameObject = new GameObject("AddressableDataCore");
                    instance = gameObject.AddComponent<AddressableDataCore>();
                    DontDestroyOnLoad(gameObject);
                }
                return instance;
            }
        }

        public IAddressableDataContainer DataContainer => dataContainer;

        public static AddressableObject<T> CreateAddressable<T>(string path) where T : UnityEngine.Object
        {
            return new AddressableObject<T>(path);
        }

        public static AddressableObject<T> CreateAddressableLoad<T>(string path,Action<T> action) where T : UnityEngine.Object
        {
            var result = new AddressableObject<T>(path);
            result.LoadAsync(action).Forget();
            return result;
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (dataContainer == null)
            {
                dataContainer = new AddressableDataContainer();
            }

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected virtual void Start()
        {
            AutoReleaseRoutine(cts.Token).Forget();
        }

        protected virtual void OnDestroy()
        {
            cts.Cancel();
            cts.Dispose();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Resources.UnloadUnusedAssets();
            if (instance == this) instance = null;
        }

        private async UniTaskVoid AutoReleaseRoutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    dataContainer?.AutoRelease();
                    Debug.Log(dataContainer?.GetGroupStats());
                    await UniTask.Delay(1000, delayTiming: PlayerLoopTiming.Update, cancellationToken: token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Debug.LogError($"AutoRelease error: {ex.Message}");
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (sceneDataMap.TryGetValue(scene.buildIndex, out var groupMap))
            {
                foreach (var list in groupMap.Values)
                {
                    foreach (var data in list)
                    {
                        data.Release();
                    }
                    list.Clear();
                }
                groupMap.Clear();
                sceneDataMap.Remove(scene.buildIndex);
            }
        }

        /// <summary>
        /// Cancels the auto-release routine.
        /// </summary>
        public void CancelAutoRelease()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Adds addressable data to the specified group and category.
        /// </summary>
        public void AddAddressableData(GroupCategory group, AssetCategory category, BaseAddressableData data, Scene? sceneLink = null)
        {
            if (data == null)
            {
                Debug.LogWarning("Attempted to add null data.");
                return;
            }
            if (!Enum.IsDefined(typeof(GroupCategory), group))
            {
                Debug.LogError($"Invalid group: {group}");
                throw new ArgumentException("Invalid GroupCategory.");
            }
            if (!Enum.IsDefined(typeof(AssetCategory), category))
            {
                Debug.LogError($"Invalid category: {category}");
                throw new ArgumentException("Invalid AssetCategory.");
            }

            data.SceneLink = sceneLink;
            if (sceneLink.HasValue)
            {
                if (!sceneDataMap.TryGetValue(sceneLink.Value.buildIndex, out var groupMap))
                {
                    groupMap = new Dictionary<GroupCategory, List<BaseAddressableData>>();
                    sceneDataMap[sceneLink.Value.buildIndex] = groupMap;
                }
                if (!groupMap.TryGetValue(group, out var list))
                {
                    list = new List<BaseAddressableData>();
                    groupMap[group] = list;
                }
                list.Add(data);
            }
            dataContainer?.Add(group, category, data);
        }

        /// <summary>
        /// Finds addressable data by index in the specified group and category.
        /// </summary>
        public BaseAddressableData Find(GroupCategory group, AssetCategory category, int index)
        {
            if (!Enum.IsDefined(typeof(GroupCategory), group) || !Enum.IsDefined(typeof(AssetCategory), category))
            {
                Debug.LogWarning($"Invalid group {group} or category {category}");
                return null;
            }
            return dataContainer?.Find(group, category, index);
        }

        /// <summary>
        /// Finds addressable data by reference across all groups and categories.
        /// </summary>
        public BaseAddressableData Find(BaseAddressableData data)
        {
            return dataContainer?.Find(data);
        }

        /// <summary>
        /// Releases all data in the specified group.
        /// </summary>
        public void ReleaseGroup(GroupCategory group)
        {
            dataContainer?.ReleaseGroup(group);
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Releases all data in the specified group and category.
        /// </summary>
        public void ReleaseCategory(GroupCategory group, AssetCategory category)
        {
            dataContainer?.ReleaseCategory(group, category);
            Resources.UnloadUnusedAssets();
        }
    }
}