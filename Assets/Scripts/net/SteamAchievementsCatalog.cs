using Steamworks;
using UnityEngine;

public static class SteamAchievementsCatalog {
    public const string Plan = "ACH_THIS_WAS_PLAN";
    public const string SkillIssue = "ACH_SKILL_ISSUE";
    public const string NotLikeThis = "ACH_NOT_LIKE_THIS";
    public const string ClownFiesta = "ACH_CLOWN_FIESTA";
    public const string WizardDiff = "ACH_WIZARD_DIFF";
    public const string Godlike = "ACH_GODLIKE";
    public const string PressF = "ACH_PRESS_F";
    public const string RapGod = "ACH_RAP_GOD";
    public const string OneHpClutch = "ACH_ONE_HP_CLUTCH";
    public const string EchoRound = "ACH_ECHO_ROUND";
    public const string Overkill = "ACH_OVERKILL";
    public const string ChainReaction = "ACH_CHAIN_REACTION";
    public const string ArcaneBlender = "ACH_ARCANE_BLENDER";
    public const string PacifistRun = "ACH_PACIFIST_RUN";
    public const string UnoReverse = "ACH_UNO_REVERSE";
    public const string PhysicsExeStopped = "ACH_PHYSICS_EXE_STOPPED_WORKING";
    public const string Parkour80 = "ACH_PARKOUR_80_LVL";
    public const string ToBeyond = "ACH_TO_BEYOND";
}

public static class SteamAchievementsClient {
    public static void Unlock(string achievementId) {
        if (string.IsNullOrEmpty(achievementId)) return;
        if (!SteamClient.IsValid) return;

        foreach (var achievement in SteamUserStats.Achievements) {
            if (achievement.Identifier != achievementId)
                continue;

            achievement.Trigger();
            SteamUserStats.StoreStats();
            Debug.Log($"[SteamAchievements] Unlocked {achievementId}");
            return;
        }

        Debug.LogWarning($"[SteamAchievements] Achievement id not found: {achievementId}");
    }
}


