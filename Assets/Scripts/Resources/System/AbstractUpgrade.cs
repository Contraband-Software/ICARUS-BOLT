using UnityEngine;

namespace Resources.System
{
    public abstract class AbstractUpgrade : ScriptableObject
    {
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public string FriendlyName { get; private set; }
        [field: SerializeField] public string Tooltip { get; private set; }
    }
}