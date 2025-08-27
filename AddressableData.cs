using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace AddressableSystem
{
    /// <summary>
    /// �^ T ���󂯎��ALoad ���� Action<T> ���ĂԃV���v�������B
    /// BaseAddressableData �͊Ǘ��p�ŁA������ Load ���\�b�h�����J����ioverride �ł͂Ȃ��j�B
    /// </summary>
    public class AddressableData<T> : BaseAddressableData where T : UnityEngine.Object
    {
        private bool isInstantiated;
        private AsyncOperationHandle<T> handle;
        private AsyncOperationHandle<IList<T>> arrayHandle;

        protected T typedAddressableObject;
        protected T[] typedAddressableArray;

        public AddressableData(GroupCategory group, AssetCategory category, Scene? sceneLink = null)
            : base(group, category, sceneLink)
        {
        }

        /// <summary>
        /// �P�̃��[�h�BonSuccess �ɓǂݍ��܂ꂽ T ��n���i�����_�̈����� T �^�j�B
        /// </summary>
        public async UniTask LoadAsync(string path, Action<T> onSuccess = null, Action<Exception> onError = null)
        {
            if (isLoaded || isSetup || string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Cannot load: Already loaded/setup or invalid path: {path}");
                onError?.Invoke(new InvalidOperationException("Invalid load state or path"));
                return;
            }

            isLoaded = true;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(path);
                typedAddressableObject = await handle.ToUniTask();
                addressableObject = typedAddressableObject;
                await UniTask.Yield(PlayerLoopTiming.Update);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    typedAddressableArray = new T[] { typedAddressableObject };
                    addressableArray = new UnityEngine.Object[] { addressableObject };
                    isSetup = true;
                    onSuccess?.Invoke(typedAddressableObject);
                }
                else
                {
                    Debug.LogError($"Failed to load asset at {path}: {handle.OperationException}");
                    isLoaded = false;
                    onError?.Invoke(handle.OperationException);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception loading asset at {path}: {ex.Message}");
                isLoaded = false;
                onError?.Invoke(ex);
            }
        }

        /// <summary>
        /// �z�񃍁[�h�BonSuccess �� IList<T> ��n���i�����_�̈����� IList<T>�j�B
        /// </summary>
        public async UniTask LoadArrayAsync(string path, Action<IList<T>> onSuccess = null, Action<Exception> onError = null)
        {
            if (isLoaded || isSetup || string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Cannot load array: Already loaded/setup or invalid path: {path}");
                onError?.Invoke(new InvalidOperationException("Invalid load state or path"));
                return;
            }

            isLoaded = true;
            try
            {
                arrayHandle = Addressables.LoadAssetAsync<IList<T>>(path);
                var result = await arrayHandle.ToUniTask();
                await UniTask.Yield(PlayerLoopTiming.Update);

                if (arrayHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    int cnt = result?.Count ?? 0;
                    typedAddressableArray = new T[cnt];
                    addressableArray = new UnityEngine.Object[cnt];

                    for (int i = 0; i < cnt; i++)
                    {
                        typedAddressableArray[i] = result[i];
                        addressableArray[i] = result[i];
                    }

                    typedAddressableObject = typedAddressableArray.Length > 0 ? typedAddressableArray[0] : null;
                    addressableObject = typedAddressableObject;
                    isSetup = true;
                    isArray = true;

                    onSuccess?.Invoke(result);
                }
                else
                {
                    Debug.LogError($"Failed to load array at {path}: {arrayHandle.OperationException}");
                    isLoaded = false;
                    onError?.Invoke(arrayHandle.OperationException);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception loading array at {path}: {ex.Message}");
                isLoaded = false;
                onError?.Invoke(ex);
            }
        }

        /// <summary>
        /// GameObject �̏ꍇ�ɃC���X�^���X�����ĕԂ��i������ T ���Q�Ɓj
        /// </summary>
        public GameObject Instantiate(string name = null)
        {
            if (!isSetup || typedAddressableObject == null || typedAddressableObject is not GameObject || isInstantiated)
            {
                Debug.LogWarning("Cannot instantiate: Asset not loaded, not a GameObject, or already instantiated.");
                return null;
            }

            isInstantiated = true;
            EnableAutoRelease();
            var instantiated = GameObject.Instantiate(typedAddressableObject as GameObject);
            if (!string.IsNullOrEmpty(name)) instantiated.name = name;
            return instantiated;
        }

        /// <summary>
        /// Release ����
        /// </summary>
        public override void Release()
        {
            if (!IsLoadedAndSetup) return;

            if (!isArray && typedAddressableObject != null && handle.IsValid())
            {
                Addressables.Release(handle);
            }
            else if (isArray && typedAddressableArray != null && arrayHandle.IsValid())
            {
                if (typeof(T) == typeof(GameObject))
                {
                    foreach (var obj in addressableArray)
                    {
                        if (obj is GameObject gameObject)
                        {
                            Addressables.ReleaseInstance(gameObject);
                        }
                    }
                }
                Addressables.Release(arrayHandle);
            }

            // ���[�U�̗v�]�ǂ���F�߂�l�𖳎����ČĂԂ����i���ɂ���Ă� void �����j
            if (addressableObject != null)
            {
                try
                {
                    Addressables.ClearDependencyCacheAsync(addressableObject.name);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception while calling ClearDependencyCacheAsync for {addressableObject.name}: {ex.Message}");
                }
            }

            addressableObject = null;
            addressableArray = null;
            typedAddressableObject = null;
            typedAddressableArray = null;
            isSetup = false;
            isLoaded = false;
            isInstantiated = false;

            Resources.UnloadUnusedAssets();
        }
    }
}
