

namespace Resources.System
{
    public interface IStatChange
    {
        float GetStatChange(Stat stat, int onTier);
        float GetStatMultiplier(Stat stat, int onTier);
    }
}