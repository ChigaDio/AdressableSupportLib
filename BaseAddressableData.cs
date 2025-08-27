using UnityEngine;
using UnityEngine.SceneManagement;

namespace AddressableSystem
{
    /// <summary>
    /// 管理用の非ジェネリック基底。
    /// ロードメソッド自体は派生側に実装させる（T 型のラムダを受け取る）。
    /// </summary>
    public abstract class BaseAddressableData
    {
        protected bool isArray;
        protected bool isSetup;
        protected bool isLoaded;
        protected bool isAutoRelease;
        protected bool isUsed;

        protected UnityEngine.Object addressableObject;
        protected UnityEngine.Object[] addressableArray;

        public Scene? SceneLink { get; set; }

        protected BaseAddressableData(GroupCategory group, AssetCategory category, Scene? sceneLink = null)
        {
            SceneLink = sceneLink;
            AddressableDataCore.Instance.AddAddressableData(group, category, this, sceneLink);
        }

        public bool IsLoadedAndSetup => isSetup && isLoaded;
        public bool IsAutoRelease => isAutoRelease;
        public UnityEngine.Object GetAddressableObject() => addressableObject;
        public UnityEngine.Object[] GetAddressableArray() => addressableArray;
        public int GetArrayCount() => addressableArray?.Length ?? 0;

        public void EnableAutoRelease() => isAutoRelease = true;
        public void MarkAsUsed() => isUsed = true;

        /// <summary>
        /// 派生で実装する（型付きの Load/LoadArray を実装すること）。
        /// </summary>
        public abstract void Release();
    }
}

