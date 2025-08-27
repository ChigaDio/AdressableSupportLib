using UnityEngine;
using UnityEngine.SceneManagement;

namespace AddressableSystem
{
    /// <summary>
    /// �Ǘ��p�̔�W�F�l���b�N���B
    /// ���[�h���\�b�h���͔̂h�����Ɏ���������iT �^�̃����_���󂯎��j�B
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
        /// �h���Ŏ�������i�^�t���� Load/LoadArray ���������邱�Ɓj�B
        /// </summary>
        public abstract void Release();
    }
}

