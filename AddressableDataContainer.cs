using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
namespace AddressableSystem
{
    /// <summary>
    /// Defines categories for addressable assets.
    /// </summary>
    public enum AssetCategory
    {
        Prefab,
        Texture,
        Audio,
        UI,
        Other
    }

    /// <summary>
    /// Defines groups for addressable assets (e.g., game modes or scenes).
    /// </summary>
    public enum GroupCategory
    {
        Title,
        Game,
        Exit,
        Menu,
        Other
    }

    /// <summary>
    /// Interface for addressable data container.
    /// </summary>
    public interface IAddressableDataContainer
    {
        int Count { get; }
        int GetGroupCount(GroupCategory group);
        int GetCategoryCount(GroupCategory group, AssetCategory category);
        void Add(GroupCategory group, AssetCategory category, BaseAddressableData data);
        BaseAddressableData Find(GroupCategory group, AssetCategory category, int index);
        BaseAddressableData Find(BaseAddressableData data);
        void AutoRelease();
        void ReleaseGroup(GroupCategory group);
        void ReleaseCategory(GroupCategory group, AssetCategory category);
        string GetGroupStats();
    }

    /// <summary>
    /// Manages a collection of BaseAddressableData instances, organized by group and category.
    /// </summary>
    public class AddressableDataContainer : IAddressableDataContainer
    {
        private readonly Dictionary<GroupCategory, Dictionary<AssetCategory, List<BaseAddressableData>>> groupDataMap =
            new Dictionary<GroupCategory, Dictionary<AssetCategory, List<BaseAddressableData>>>();

        public int Count
        {
            get
            {
                int total = 0;
                foreach (var group in groupDataMap.Values)
                {
                    foreach (var list in group.Values)
                    {
                        total += list?.Count ?? 0;
                    }
                }
                return total;
            }
        }

        public int GetGroupCount(GroupCategory group)
        {
            if (groupDataMap.TryGetValue(group, out var categoryMap))
            {
                int total = 0;
                foreach (var list in categoryMap.Values)
                {
                    total += list?.Count ?? 0;
                }
                return total;
            }
            return 0;
        }

        public int GetCategoryCount(GroupCategory group, AssetCategory category)
        {
            if (groupDataMap.TryGetValue(group, out var categoryMap) &&
                categoryMap.TryGetValue(category, out var list))
            {
                return list?.Count ?? 0;
            }
            return 0;
        }

        public void Add(GroupCategory group, AssetCategory category, BaseAddressableData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Attempted to add null data to AddressableDataContainer.");
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

            if (!groupDataMap.TryGetValue(group, out var categoryMap))
            {
                categoryMap = new Dictionary<AssetCategory, List<BaseAddressableData>>();
                groupDataMap[group] = categoryMap;
            }
            if (!categoryMap.TryGetValue(category, out var list))
            {
                list = new List<BaseAddressableData>();
                categoryMap[category] = list;
            }
            list.Add(data);
        }

        public BaseAddressableData Find(GroupCategory group, AssetCategory category, int index)
        {
            if (!Enum.IsDefined(typeof(GroupCategory), group) || !Enum.IsDefined(typeof(AssetCategory), category))
            {
                Debug.LogWarning($"Invalid group {group} or category {category} for AddressableDataContainer.Find.");
                return null;
            }
            if (!groupDataMap.TryGetValue(group, out var categoryMap) ||
                !categoryMap.TryGetValue(category, out var list) || list == null || index < 0 || index >= list.Count)
            {
                Debug.LogWarning($"Invalid group {group}, category {category}, or index {index} for AddressableDataContainer.Find.");
                return null;
            }
            return list[index];
        }

        public BaseAddressableData Find(BaseAddressableData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Attempted to find null data in AddressableDataContainer.");
                return null;
            }

            foreach (var categoryMap in groupDataMap.Values)
            {
                foreach (var list in categoryMap.Values)
                {
                    if (list != null)
                    {
                        var found = list.Find(item => item == data);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }
            Debug.LogWarning("Data not found in AddressableDataContainer.");
            return null;
        }

        public void AutoRelease()
        {
            foreach (var groupKvp in groupDataMap)
            {
                var categoryMap = groupKvp.Value;
                foreach (var categoryKvp in categoryMap)
                {
                    var list = categoryKvp.Value;
                    if (list == null || list.Count == 0) continue;

                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var data = list[i];
                        if (data.IsAutoRelease && data.IsLoadedAndSetup && data.GetAddressableObject() == null)
                        {
                            data.Release();
                            list.RemoveAt(i);
                        }
                    }

                    if (list.Count == 0)
                    {
                        categoryMap.Remove(categoryKvp.Key);
                    }
                    else
                    {
                        list.TrimExcess();
                    }
                }

                if (categoryMap.Count == 0)
                {
                    groupDataMap.Remove(groupKvp.Key);
                }
            }
        }

        public void ReleaseGroup(GroupCategory group)
        {
            if (!groupDataMap.TryGetValue(group, out var categoryMap) || categoryMap == null)
            {
                Debug.LogWarning($"No data found for group {group} in AddressableDataContainer.");
                return;
            }

            foreach (var list in categoryMap.Values)
            {
                foreach (var data in list)
                {
                    data.Release();
                }
                list.Clear();
            }
            categoryMap.Clear();
            groupDataMap.Remove(group);
        }

        public void ReleaseCategory(GroupCategory group, AssetCategory category)
        {
            if (!groupDataMap.TryGetValue(group, out var categoryMap) ||
                !categoryMap.TryGetValue(category, out var list) || list == null)
            {
                Debug.LogWarning($"No data found for group {group}, category {category} in AddressableDataContainer.");
                return;
            }

            foreach (var data in list)
            {
                data.Release();
            }
            list.Clear();
            categoryMap.Remove(category);
            if (categoryMap.Count == 0)
            {
                groupDataMap.Remove(group);
            }
        }

        public string GetGroupStats()
        {
            var stats = new StringBuilder("AddressableDataContainer Stats:\n");
            foreach (var groupKvp in groupDataMap)
            {
                stats.AppendLine($"Group: {groupKvp.Key}, Total Count: {GetGroupCount(groupKvp.Key)}");
                foreach (var categoryKvp in groupKvp.Value)
                {
                    stats.AppendLine($"  Category: {categoryKvp.Key}, Count: {categoryKvp.Value?.Count ?? 0}, Loaded: {categoryKvp.Value?.Count(d => d.IsLoadedAndSetup) ?? 0}");
                }
            }
            return stats.ToString();
        }
    }
}