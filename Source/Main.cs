﻿using Harmony;
using RimWorld;
using System.Reflection;
using System.Text;
using Verse;

namespace ModifyResearchTime
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.modifyresearchtime.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Log.Message("ChangeResearchTime: Adding Harmony Postfix to Game.InitNewGame");
            Log.Message("ChangeResearchTime: Adding Harmony Postfix to Root.Shutdown");
            Log.Message("ChangeResearchTime: Adding Harmony Postfix to ResearchManager.ReapplyAllMods");
        }
    }

    [HarmonyPatch(typeof(Game), "InitNewGame")]
    static class Patch_Game_InitNewGame
    {
        static void Prefix()
        {
#if DEBUG
            Log.Warning("Patch_Game_InitNewGame Postfix");
#endif
            WorldComp.InitializeNewGame();
        }
    }

    [HarmonyPatch(typeof(Root), "Shutdown")]
    static class Patch_Page_SelectScenario_PreOpen
    {
        static void Postfix()
        {
#if DEBUG
            Log.Warning("Patch_Page_SelectScenario_PreOpen Postfix");
#endif
            ResearchTimeUtil.ResetResearchFactor();
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "ReapplyAllMods")]
    static class Patch_ResearchManager_ReapplyAllMods
    {
        static void Postfix()
        {
            if (Settings.AllowTechAdvance == false)
            {
                return;
            }

            TechLevel currentTechLevel = Faction.OfPlayer.def.techLevel;
#if DEBUG
            Log.Warning("Tech Level: " + techLevel);
#endif
            int totalCurrentAndPast = 0;
            int finishedResearch = 0;

            foreach (ResearchProjectDef def in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (def.IsFinished)
                {
                    ++finishedResearch;
                    //if (def.techLevel <= currentTechLevel)
                    //    ++finishedCurrentAndPast;
                    //else
                    //    ++finishedFuture;
                }
                
                if (def.techLevel <= currentTechLevel)
                {
                    ++totalCurrentAndPast;
                }
            }
#if DEBUG
            Log.Warning("Finished: " + countCurrentAndPreviousTechLevelFinished + " Next Tech Level Finished: " + countNextTechLevelFinished + " Total Techs: " + totalCurrentAndPreviousTechLevel);
#endif
            int neededTechsToAdvance = 0;
            bool useStatisPerTier = Settings.StaticNumberResearchPerTier;
            if (useStatisPerTier)
            {
                switch(Faction.OfPlayer.def.techLevel)
                {
                    case TechLevel.Neolithic:
                        neededTechsToAdvance = Settings.NeolithicNeeded;
                        break;
                    case TechLevel.Medieval:
                        neededTechsToAdvance = Settings.MedievalNeeded;
                        break;
                    case TechLevel.Industrial:
                        neededTechsToAdvance = Settings.IndustrialNeeded;
                        break;
                    case TechLevel.Spacer:
                        neededTechsToAdvance = Settings.SpacerNeeded;
                        break;
                }
            }

#if DEBUG
            Log.Warning("Current Tech Level: " + Faction.OfPlayer.def.techLevel);
            Log.Warning("neededTechsToAdvance: " + neededTechsToAdvance);
            Log.Warning("totalCurrentAndPast: " + totalCurrentAndPast);
            Log.Warning("finishedResearch: " + finishedResearch);
#endif

            if ((useStatisPerTier && neededTechsToAdvance < finishedResearch) || 
                (!useStatisPerTier && totalCurrentAndPast + 1 < finishedResearch))
            {
                if (Scribe.mode == LoadSaveMode.Inactive)
                {
                    // Only display this message is not loading
                    Messages.Message("Advancing Tech Level from [" + currentTechLevel.ToString() + "] to [" + (currentTechLevel + 1).ToString() + "].", MessageTypeDefOf.PositiveEvent);
                }
                Faction.OfPlayer.def.techLevel = currentTechLevel + 1;
            }
            else
            {
                int needed = (useStatisPerTier) ? neededTechsToAdvance - finishedResearch : totalCurrentAndPast + 1 - finishedResearch;
                Log.Message("Tech Advance: Need to research [" + needed + "] more technologies");
            }
        }
    }
}
