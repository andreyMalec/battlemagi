public static class HitOutcomeRules {
    public static bool CanApply(HitOutcome outcome, HitOutcome add) {
        if (add == HitOutcome.Bounce)
            return (outcome & HitOutcome.Pierce) == 0;

        if (add == HitOutcome.Pierce)
            return (outcome & HitOutcome.Bounce) == 0;

        return true;
    }
}

