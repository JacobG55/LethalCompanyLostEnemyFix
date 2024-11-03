using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace LostEnemyFix.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        public static readonly Dictionary<EnemyAI, int> EnemyErrors = new Dictionary<EnemyAI, int>();

        public static bool SetAgentDestination(EnemyAI enemy)
        {
            bool value = enemy.agent.SetDestination(enemy.destination);

            if (!value)
            {
                bool kill = false;
                switch (LostEnemyFix.killLostEnemiesMode.Value)
                {
                    case LostEnemyFix.KillLostEnemies.Reasonable:
                        if (EnemyErrors.ContainsKey(enemy))
                        {
                            EnemyErrors[enemy]++;
                            if (EnemyErrors[enemy] > 16)
                            {
                                kill = true;
                            }
                        }
                        else
                        {
                            EnemyErrors.Add(enemy, 1);
                        }
                        break;
                    case LostEnemyFix.KillLostEnemies.Aggressive:
                        kill = true;
                        break;
                    default:
                        break;
                }

                LostEnemyFix.Instance.mls.LogWarning($"{(enemy.enemyType == null ? enemy.name : enemy.enemyType.enemyName)} Failed to locate NavMesh. {(kill ? "Killing enemy to prevent further errors.\n" : "\n")}" +
                    $"Enemy Position: {enemy.transform.position} Target Destination: {enemy.destination} IsOutside: {enemy.isOutside} State: {enemy.currentBehaviourState.name}");

                if (kill)
                {
                    enemy.KillEnemyServerRpc(true);
                }
            }

            return value;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo SetDestinationCall = AccessTools.Method(typeof(NavMeshAgent), nameof(NavMeshAgent.SetDestination), new System.Type[] { typeof(Vector3) });
            MethodInfo ReplacementMethod = AccessTools.Method(typeof(EnemyAIPatch), nameof(SetAgentDestination), new System.Type[] { typeof(EnemyAI) });

            bool found = false;

            List<CodeInstruction> newInstructions = new List<CodeInstruction>();

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && instruction.Calls(SetDestinationCall))
                {
                    found = true;
                    newInstructions.RemoveAt(newInstructions.Count - 1);
                    newInstructions.RemoveAt(newInstructions.Count - 1);
                    newInstructions.RemoveAt(newInstructions.Count - 1);
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, ReplacementMethod));
                }
                else
                {
                    newInstructions.Add(instruction);
                }
            }

            foreach (CodeInstruction instruction in newInstructions)
            {
                yield return instruction;
            }

            if (!found) LostEnemyFix.Instance.mls.LogInfo("Failed to patch EnemyAI!");
        }
    }
}
