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
     * Which method to patch? Prefix SefiraPanel.Init
     */
    // The class containing your Harmony Patches has to be named Harmony_Patch for BaseMod/LMM to load it
    public class Harmony_Patch
    {
        // BaseMod/LMM will run the code in the constructor for the Harmony_Patch class
        public Harmony_Patch()
        {
            try
            {
                /* You must create a HarmonyInstance using HarmonyInstance.Create. The name has to be unique.
                The HarmonyInstance is what allows you to do Harmony patches. */
                HarmonyInstance HInstance = HarmonyInstance.Create("VBlankFF_FixMissingContainmentUnits");
                // To use a method as a patch you have to make it a Harmony Method first. Here I get the method by name.
                HarmonyMethod fillEmptySlots = new HarmonyMethod(typeof(Harmony_Patch).GetMethod("FillEmptyCells"));
                /* This is what patches the code of the game. The parameters are the method in the game you want to change, 
                and a Prefix, a Postfix, and a Transpiler (each are HarmonyMethods). Prefixes are run before the game's code, Postfixes are run after,
                and Transpilers are their own, more complicated thing that I won't explain here. null just means you aren't applying that kind of patch.
                Here a Prefix is applied, and no Postfix or Transpiler is applied. */
                HInstance.Patch(typeof(SefiraPanel).GetMethod("Init"), fillEmptySlots, null, null);
                // This is just here for the log, it's not required for patching at all
                UnityEngine.Debug.Log("FixMissingContainmentUnits load success");
            }
            /* If your patching code throws an error and you do not catch it it will fail to load and the game will immediately stop loading mods. This will mess with other mods,
            and the player will not be notified of the load issue at all unless they have LoadErrorCheck.*/
            catch (Exception e)
            {
                // Catching the exception might prevent LoadErrorCheck from catching it, though...
                // I think it's still worth doing because a lot of people do not have LoadErrorCheck anyway
                UnityEngine.Debug.Log("FixMissingContainmentCells: An error has occured during load: " + e.ToString());
            }
        }
        // All Harmony Patches (or at least Prefixes and Postfixes) must be static and probably public?
        // This method is a Prefix for SefiraPanel.Init(SefiraEnum sefira). A Prefix can use the parameters passed to the original method if you put them in your patch and they have
        // the same name and type.
        public static void FillEmptyCells(SefiraEnum sefira)
        {
            try
            {
                UnityEngine.Debug.Log("FixMissingContainmentCells: Start");
                Sefira sephirot = SefiraManager.instance.GetSefira(sefira);
                int numCreatures;
                if (!sephirot.activated || sephirot.sefiraEnum == SefiraEnum.DAAT)
                {
                    return;
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
                UnityEngine.Debug.Log("FixMissingContainmentCells: Finished");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("FixMissingContainmentCells: An error has occured: " + e.ToString());
            }
        }
        // This is not a Harmony patch, it's just a method called by FillEmptyCells()
        public static int FindNumCreatures(Sefira sephirot)
        {
            // check for energy corp
            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assem.FullName.Contains("Qquickness_EnergyCo_MOD"))
                {
                    // can't confidently say how many abnos should be in an incomplete department
                    if (sephirot.openLevel < 5)
                    {
                        return 0;
                    }
                    switch (sephirot.sefiraEnum)
                    {
                        case SefiraEnum.MALKUT:
                            return 12;
                        case SefiraEnum.YESOD:
                        case SefiraEnum.NETZACH: 
                        case SefiraEnum.HOD:
                        case SefiraEnum.BINAH:
                        case SefiraEnum.CHOKHMAH:
                            return 8;
                        case SefiraEnum.TIPERERTH1:
                        case SefiraEnum.TIPERERTH2:
                            return 10;
                        case SefiraEnum.CHESED:
                        case SefiraEnum.GEBURAH:
                            return 7;
                        case SefiraEnum.KETHER:
                            return 20;
                        default: return 0;
                    }
                }
            }    
            // no energy corp
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
        // This is not a Harmony patch, it's just a method called by FillEmptyCells()
        public static long GetRandomNewCreature()
        {
            List<long> newCreatures = new List<long>();
            foreach (long i in CreatureGenerateInfo.GetAll())
            {
                newCreatures.Add(i);
            }
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
                foreach (long i in CreatureGenerateInfo.GetAll())
                {
                    newCreatures.Add(i);
                }
                foreach (LcIdLong id in CreatureGenerateInfo.GetAll_Mod())
                {
                    newCreatures.Add(id.id);
                }
            }
            return newCreatures[UnityEngine.Random.Range(0, newCreatures.Count)];
        }
    }
}
