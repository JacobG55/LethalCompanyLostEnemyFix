using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LostEnemyFix.Patches;

namespace LostEnemyFix
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class LostEnemyFix : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.LostEnemyFix";
        private const string modName = "LostEnemyFix";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal ManualLogSource mls;

        public static LostEnemyFix Instance;

        public static ConfigEntry<KillLostEnemies> killLostEnemiesMode;

        public enum KillLostEnemies
        {
            Aggressive,
            Reasonable,
            Off,
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            killLostEnemiesMode = Config.Bind("Main", "Kill Lost Enemies Mode", KillLostEnemies.Reasonable, "Aggressive: Kills enemies when they throw a NavMesh error.\nReasonable: Kills enemies after they have thrown 16 NavMesh errors.\nOff: Vanilla behavior");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
        }
    }
}
