using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Helpers
{
    public interface IKeyedEntry<T>
    {
        T GetKey();
    }

    // Enforece IKeyedEntry on GroupByIndexT IndexByT

    public abstract class GeneralMixer<GroupIndexByT, IndexByT, SerializableEntryT, GroupComponentT > : MonoBehaviour
        where SerializableEntryT : struct, IKeyedEntry<IndexByT>
        where GroupComponentT : MixedValue
    {
        [SerializeField] private List<MixingGroup<GroupIndexByT, SerializableEntryT>> MixingGroups 
            = new List<MixingGroup<GroupIndexByT, SerializableEntryT>>();
        protected Dictionary<GroupIndexByT, Dictionary<IndexByT, GroupComponentT>> MixingGroupMap;

        protected void InitializeMixingGroupMap()
        {
            MixingGroupMap = new Dictionary<GroupIndexByT, Dictionary<IndexByT, GroupComponentT>>();
            foreach (var group in MixingGroups)
            {

                GroupIndexByT groupKey = group.groupKey;
                if (MixingGroupMap.ContainsKey(groupKey))
                {
                    Debug.LogWarning($"Duplicate entry for {group.groupKey} detected. Ignoring.");
                    continue;
                }

                Dictionary<IndexByT, GroupComponentT> newMixingGroup = new Dictionary<IndexByT, GroupComponentT>();

                foreach (var entry in group.entries)
                {
                    IndexByT key;
                    if (entry is IKeyedEntry<IndexByT> keyedEntry)
                    {
                        key = keyedEntry.GetKey();
                    }
                    else
                    {
                        Debug.LogError($"Entry does not implement IKeyedEntry<{typeof(IndexByT)}>");
                        continue;
                    }

                    if (newMixingGroup.ContainsKey(key))
                    {
                        Debug.LogWarning($"Duplicate entry for {key} in group {group.groupKey} detected. Ignoring.");
                        continue;
                    }
                    GroupComponentT component = LoadMixedComponent(entry);
                    newMixingGroup[keyedEntry.GetKey()] = component;
                }

                MixingGroupMap[groupKey] = newMixingGroup;
            }
        }

        protected abstract GroupComponentT LoadMixedComponent(SerializableEntryT entry);

        protected bool CheckGroupExistence(GroupIndexByT group)
        {
            if (!MixingGroupMap.ContainsKey(group))
            {
                Debug.LogWarning($"Group of name: " + group + " Does not exist. Ignoring.");
                return false;
            }
            return true;
        }

        protected bool CheckExistenceInMap(GroupIndexByT group, IndexByT component)
        {
            if (!CheckGroupExistence(group)) return false;
            if (!MixingGroupMap[group].ContainsKey(component))
            {
                Debug.LogWarning($"Group of name: " + group + " Does not have component of name: " + component + ". Ignoring.");
                return false;
            }
            return true;
        }

        public GroupComponentT GetMixedComponent(GroupIndexByT group, IndexByT component)
        {
            if (!CheckExistenceInMap(group, component)) return null;
            return MixingGroupMap[group][component];
        }

        public void StartFade(GroupIndexByT group, IndexByT component, float target, float? rate = null, Easing.EaseType? easeType = null)
        {
            if (!CheckExistenceInMap(group, component)) return;
            MixingGroupMap[group][component].StartFade(target, rate, easeType);
        }

        public void StartDefaultFadeOut(GroupIndexByT group, IndexByT component)
        {
            if (!CheckExistenceInMap(group, component)) return;
            MixingGroupMap[group][component].StartDefaultFadeOut();
        }

        public void SetComponentValueAndStopFade(GroupIndexByT group, IndexByT component, float v)
        {
            if (!CheckExistenceInMap(group, component)) return;
            MixingGroupMap[group][component].SetValueAndStopFade(v);
        }

        public void HardResetAllOfGroup(GroupIndexByT group)
        {
            if(!CheckGroupExistence(group)) return;
            foreach(IndexByT component in MixingGroupMap[group].Keys)
            {
                MixingGroupMap[group][component].SetValueAndStopFade(0f);
            }
        }
    }
}
