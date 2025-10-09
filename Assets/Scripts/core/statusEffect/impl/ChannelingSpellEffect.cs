using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/ChannelingSpell")]
public class ChannelingSpellEffect : StatusEffectData {
    public float speedMultiplier = 0.3f;

    public override StatusEffectRuntime CreateRuntime() {
        return new ChannelingSpellRuntime(this);
    }

    public override int CompareTo(StatusEffectData other) {
        if (other is ChannelingSpellEffect effect) {
            return speedMultiplier.CompareTo(effect.speedMultiplier);
        }

        return RESET_TIME;
    }

    private class ChannelingSpellRuntime : StatusEffectRuntime {
        private readonly ChannelingSpellEffect _data;

        public ChannelingSpellRuntime(ChannelingSpellEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(ulong ownerClientId, GameObject target) {
            base.OnApply(ownerClientId, target);
            var statSystem = target.GetComponent<NetworkStatSystem>();
            if (statSystem != null) {
                statSystem.AddModifierServer(StatType.MoveSpeed, _data.speedMultiplier);
            }

            target.GetComponent<StateController>().SetChanneling(true);
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            var statSystem = target.GetComponent<NetworkStatSystem>();
            if (statSystem != null) {
                statSystem.RemoveModifierServer(StatType.MoveSpeed, _data.speedMultiplier);
            }

            target.GetComponent<StateController>().SetChanneling(false);
        }
    }
}