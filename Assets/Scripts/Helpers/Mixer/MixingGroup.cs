using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers
{
    [Serializable]
    public class MixingGroup<GroupIndexByT, SerializableEntry>
        where SerializableEntry : struct
    {
        public GroupIndexByT groupKey;
        public List<SerializableEntry> entries = new List<SerializableEntry>();
    }
}
