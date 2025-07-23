using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using LobotomyBaseMod;
using UnityEngine;

namespace FixEmptyContainmentCells
{
    /* Current approach: if there's less abnormities than there should be given the expansion level, add more in the missing slots
     * Which method to hijack? postfix CreatureManager.LoadData():
     * All the info I need should be available, seems to happen before the UI and such load?
     * How do I make sure an abno loads properly?
     * 
     * Does this break with energy corp? (probably)
     * How to determine which abno to add?
     * Where to get all this info from?
     * Can you bring up an abno choice screen for this without it breaking everything? (doubtful)
     */
    public class Harmony_Patch
    {
        public Harmony_Patch()
        {
            HarmonyInstance HInstance = HarmonyInstance.Create("VBlankFF_FixMissingContainmentUnits");
            HarmonyMethod fillEmptySlots = new HarmonyMethod(typeof(Harmony_Patch).GetMethod("FindEmptyCells"));
            HInstance.Patch(typeof(CreatureManager).GetMethod("LoadData"), null, fillEmptySlots, null);
        }
        public static void FillEmptyCells()
        {
            try
            {
                UnityEngine.Debug.Log("FixMissingContainmentCells: Start");
                int numCreatures;
                foreach (Sefira sephirot in SefiraManager.instance.sefiraList)
                {
                    if (!sephirot.activated || sephirot.sefiraEnum == SefiraEnum.DAAT)
                    {
                        continue;
                    }
                    numCreatures = FindNumCreatures(sephirot);
                    for (int i = sephirot.creatureList.Count; i < numCreatures; i++)
                    {
                        SefiraIsolate emptyRoom = sephirot.isolateManagement.GetNotUsed();
                        if (emptyRoom is null)
                        {
                            UnityEngine.Debug.Log("FixMissingContainmentCells: Detected missing abnormality in " + sephirot.name + " but there's no empty rooms? Skipping");
                            continue;
                        }
                        CreatureModel addedCreature = CreatureManager.instance.AddCreature(GetRandomNewCreature(), emptyRoom, sephirot.indexString);
                        UnityEngine.Debug.Log("FixMissingContainmentCells: Added " + addedCreature.GetUnitName() + " to " + sephirot.name);
                    }
                }
                UnityEngine.Debug.Log("FixMissingContainmentCells: Finished");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("FixMissingContainmentCells: An error has occured: " + e.ToString());
            }
        }
        public static int FindNumCreatures(Sefira sephirot)
        {
            switch (sephirot.sefiraEnum)
            {
                case SefiraEnum.TIPERERTH1:
                    return Math.Min(sephirot.openLevel * 2, 4);
                case SefiraEnum.TIPERERTH2:
                    return Math.Min(Math.Max((sephirot.openLevel - 2) * 2, 0), 4);
                case SefiraEnum.KETHER:
                    return Math.Min(sephirot.openLevel * 2, 8);
                default:
                    return Math.Min(sephirot.openLevel, 4);
            }
        }
        public static long GetRandomNewCreature()
        {
            List<long> newCreatures = new List<long>();
            newCreatures = (List<long>)newCreatures.Concat(CreatureGenerateInfo.GetAll());
            foreach (LcIdLong id in CreatureGenerateInfo.GetAll_Mod())
            {
                newCreatures.Add(id.id);
            }
            CreatureModel[] CreatureList = CreatureManager.instance.GetCreatureList();
            foreach (CreatureModel creature in CreatureList)
            {
                if (creature is null)
                {
                    UnityEngine.Debug.Log("FixMissingContainmentCells: null found in CreatureList?");
                    continue;
                }
                newCreatures.Remove(creature.metadataId);
            }
            if (newCreatures.Count == 0)
            {
                UnityEngine.Debug.Log("FixMissingContainmentCells: No unique creatures left");
                return 100009L;
            }
            return newCreatures[UnityEngine.Random.Range(0, newCreatures.Count)];
        }
    }
}
