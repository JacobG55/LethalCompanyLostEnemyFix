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
            if (enemy.agent.isOnNavMesh || enemy.agent.isOnOffMeshLink)
            {
                return enemy.agent.SetDestination(enemy.destination);
            }
            else
            {
                bool kill = false;
                switch (LostEnemyFix.killLostEnemiesMode.Value)
                {
                    case LostEnemyFix.KillLostEnemies.Reasonable:
                        if (EnemyErrors.ContainsKey(enemy))
                        {
                            EnemyErrors[enemy]++;
                            if (EnemyErrors[enemy] > 8)
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

                string enemyName = (enemy.enemyType == null ? enemy.name : enemy.enemyType.enemyName);
                LostEnemyFix.Instance.mls.LogWarning($"{enemyName} Failed to locate NavMesh.\nEnemy Position: {enemy.transform.position} Target Destination: {enemy.destination} IsOutside: {enemy.isOutside} State: {enemy.currentBehaviourState.name}");

                if (kill && enemy.IsOwner)
                {
                    if (LostEnemyFix.attemptRelocation.Value && NavMesh.SamplePosition(enemy.transform.position, out NavMeshHit hit, 50, enemy.agent.areaMask))
                    {
                        LostEnemyFix.Instance.mls.LogInfo($"Relocating {enemyName} to {hit.position} to prevent further errors.");
                        enemy.agent.Warp(hit.position);
                    }
                    else
                    {
                        LostEnemyFix.Instance.mls.LogInfo($"Killing {enemyName} to prevent further errors.");
                        enemy.KillEnemyServerRpc(true);
                    }
                }
            }
            return false;
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
