using UnityEngine;

namespace Resources.System
{
    public abstract class AbstractUpgradeAsset : ScriptableObject
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public Sprite IconLayer_1 { get; private set; }
        [field: SerializeField] public Sprite IconLayer_2 { get; private set; }
        [field: SerializeField] public string FullName { get; private set; }
        [field: SerializeField] public string FriendlyName { get; private set; }
        [field: SerializeField] public string Tooltip { get; private set; }

        public abstract float GetRarity();
        public abstract float GetRarity(int tier);
        public abstract float GetPointValue();
        public abstract float GetPointValue(int tier);
    }
}