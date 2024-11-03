using HarmonyLib;

namespace LostEnemyFix.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("ResetEnemyVariables")]
        [HarmonyPrefix]
        public static void patchResetEnemyVariables()
        {
            EnemyAIPatch.EnemyErrors.Clear();
            LostEnemyFix.Instance.mls.LogInfo("Clearing Enemy Error List");
        }
    }
}
