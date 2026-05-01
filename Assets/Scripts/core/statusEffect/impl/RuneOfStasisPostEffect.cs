using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Rune of Stasis Post-Effect")]
public class RuneOfStasisPostEffect : StatusEffectData {
    public float healthRegenMultiplier = 20f;

    public override StatusEffectRuntime CreateRuntime() {
        return new RuneOfStasisPostEffectRuntime(this);
    }

    private class RuneOfStasisPostEffectRuntime : StatusEffectRuntime {
        private readonly RuneOfStasisPostEffect _data;

        public RuneOfStasisPostEffectRuntime(RuneOfStasisPostEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            var stats = target.GetComponent<Stats>();
            if (stats != null) {
                stats.System.AddModifier(StatType.HealthRegen, _data.healthRegenMultiplier);
            }

            target.GetComponent<Player>().ApplyEffectColorClientRpc(_data.color);
            target.GetComponent<StateController>().SetFreeze(true);
            target.GetComponent<Damageable>().SetInvulnerable(true);
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            var stats = target.GetComponent<Stats>();
            if (stats != null) {
                stats.RemoveModifier(StatType.HealthRegen, _data.healthRegenMultiplier);
            }

            target.GetComponent<Player>().RemoveEffectColorClientRpc(_data.color);
            target.GetComponent<StateController>().SetFreeze(false);
            target.GetComponent<Damageable>().SetInvulnerable(false);
        }
    }
}