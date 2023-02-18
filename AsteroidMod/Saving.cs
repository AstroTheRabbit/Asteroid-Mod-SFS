using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SFS.Parsers.Json;
using SFS.WorldBase;
using SFS.World;
using SFS.IO;
using SFS.UI;

namespace AsteroidMod
{
    public class Patches
    {
        public static Dictionary<WorldSave, List<AsteroidSave>> asteroidSaves = new Dictionary<WorldSave, List<AsteroidSave>>();

        [HarmonyPatch(typeof(WorldSave), nameof(WorldSave.Save))]
        class Save
        {
            static void Postfix(FolderPath path, WorldSave worldSave)
            {
                JsonWrapper.SaveAsJson(path.ExtendToFile("Asteroids.txt"), asteroidSaves[worldSave], pretty: false);
            }
        }

        [HarmonyPatch(typeof(WorldSave), methodType: MethodType.Constructor, argumentTypes: new System.Type[]
            {
                typeof(string),
                typeof(WorldSave.CareerState),
                typeof(WorldSave.Astronauts),
                typeof(WorldSave.WorldState),
                typeof(RocketSave[]),
                typeof(Dictionary<int, SFS.Stats.Branch>),
                typeof(List<SFS.Achievements.AchievementId>)
            }
        )]
        class ConstructorLoad
        {
            static void Postfix(WorldSave __instance)
            {
                asteroidSaves.Add(__instance, new List<AsteroidSave>());
            }
        }

        [HarmonyPatch(typeof(WorldSave), nameof(WorldSave.TryLoad))]
        class TryLoad
        {
            static void Postfix(FolderPath path, WorldSave worldSave)
            {
                if (JsonWrapper.TryLoadJson(path.ExtendToFile("Asteroids.txt"), out List<AsteroidSave> loadedAsteroids))
                    asteroidSaves[worldSave] = loadedAsteroids;
                else
                    Debug.LogError("Asteroids Mod - Failed to load asteroids from save!");
            }
        }

        [HarmonyPatch(typeof(GameManager), "ClearWorld")]
        class ClearWorld
        {
            static void Postfix()
            {
                Debug.Log("clear");
                while (AsteroidManager.main.asteroids.Count > 0)
                {
                    AsteroidManager.main.asteroids[0].DestroyAsteroid(false);
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSave))]
        class WorldLoad
        {
            static void Postfix(WorldSave save)
            {
                Debug.Log("worldLoad");
                asteroidSaves[save].ForEach(a => AsteroidManager.main.LoadAsteroid(a));
                // Debug.Log(AsteroidManager.main.asteroids.Count);
            }
        }

        [HarmonyPatch(typeof(GameManager), "CreateWorldSave")]
        class CreateSave
        {
            static void Postfix(WorldSave __result)
            {
                Debug.Log("createSave");
                asteroidSaves[__result] = AsteroidManager.main.asteroids.Select(a => new AsteroidSave(a)).ToList();
            }
        }



        [HarmonyPatch(typeof(CreateWorldMenu), nameof(CreateWorldMenu.Open))]
        class AsteroidsSettingsUI
        {
            static void Prefix(CreateWorldMenu __instance)
            {
                Transform holder = __instance.GetComponentInChildren<VerticalLayout>().transform;
                SelectorSettings.SelectionUI.CreateUI(holder);
            }
        }

        [HarmonyPatch(typeof(CreateWorldMenu), nameof(CreateWorldMenu.CreateWorld))]
        class CreateWorld
        {
            static WorldReference createdWorld = null;
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var worldRefConstructor = typeof(WorldReference).GetConstructor(new System.Type[] { typeof(string) });
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.OperandIs(worldRefConstructor))
                    {
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Stsfld, typeof(CreateWorld).GetField(nameof(CreateWorld.createdWorld), BindingFlags.NonPublic | BindingFlags.Static));
                    }
                }
            }
            static void Postfix()
            {
                FilePath file = createdWorld.path.ExtendToFile("TempStartingAsteroids.txt");
                // TODO: Make request and stuff
            }
        }
    }
}