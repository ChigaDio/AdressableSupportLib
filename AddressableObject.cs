using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableSystem
{
    [System.Serializable]
    public class AddressableObject<T> where T : UnityEngine.Object
    {
        [SerializeField] private bool isSetup;
        [SerializeField] private string addressablePath;
        [SerializeField] private T loadedObject;
        private bool isLoading;

        public bool IsSetup => isSetup;
        public T LoadedObject => loadedObject;
        public string AddressablePath
        {
            get => addressablePath;
            set => addressablePath = value;
        }

        public AddressableObject(string path)
        {
            addressablePath = path;
        }


        public async UniTask<T> LoadAsync(Action<T> action = null)
        {
            if (isLoading || isSetup || string.IsNullOrEmpty(addressablePath))
            {
                return loadedObject;
            }

            isLoading = true;
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(addressablePath);
                loadedObject = await handle.ToUniTask();
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    isSetup = true;
                    action?.Invoke(loadedObject);
                    return loadedObject;
                }

                Debug.LogError($"Failed to load asset at {addressablePath}: {handle.OperationException}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception loading asset at {addressablePath}: {ex.Message}");
                return null;
            }
            finally
            {
                isLoading = false;
            }
        }

        public GameObject Instantiate(string name = null)
        {
            if (!isSetup || loadedObject == null || loadedObject is not GameObject)
            {
                Debug.LogWarning("Cannot instantiate: Asset not loaded or not a GameObject.");
                return null;
            }

            var instantiated = GameObject.Instantiate(loadedObject as GameObject, Vector3.zero, Quaternion.identity);
            if (!string.IsNullOrEmpty(name))
            {
                instantiated.name = name;
            }
            return instantiated;
        }

        public void Release()
        {
            if (!isSetup || loadedObject == null) return;

            if (loadedObject is GameObject)
            {
                Addressables.ReleaseInstance(loadedObject as GameObject);
            }
            else
            {
                Addressables.Release(loadedObject);
            }
            loadedObject = null;
            isSetup = false;
        }
    }
}