namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Optional hook used by damage instigators to modify outgoing damage before resistances are applied.
    /// </summary>
    public interface IOutgoingDamageModifier
    {
        float ModifyOutgoingDamage(Health target, float damage);
    }
}
