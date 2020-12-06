﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ProjectRimFactory.Storage;
using SimpleFixes;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModComponent : Mod
    {
        // I am happy enough to make this static; it's not like there will be more than once
        //   instance of the mod loaded or anything.
        public static List<SpecialSculpture>
            availableSpecialSculptures; // loaded on startup in SpecialScupture; see above

        public ProjectRimFactory_ModComponent(ModContentPack content) : base(content)
        {
            try
            {
                ProjectRimFactory_ModSettings.LoadXml(content);
                Settings = GetSettings<ProjectRimFactory_ModSettings>();
                HarmonyInstance = new Harmony("com.spdskatr.projectrimfactory");
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message(
                    $"Project RimFactory Core {typeof(ProjectRimFactory_ModComponent).Assembly.GetName().Version} - Harmony patches successful");
                NoMessySpawns.Instance.Add(ShouldSuppressDisplace, (Building_MassStorageUnit b, Map map) => true);
                availableSpecialSculptures = SpecialSculpture.LoadAvailableSpecialSculptures(content);
            }
            catch (Exception ex)
            {
                Log.Error("Project RimFactory Core :: Caught exception: " + ex);
            }
        }

        public Harmony HarmonyInstance { get; }

        public ProjectRimFactory_ModSettings Settings { get; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ProjectRimFactoryModName".Translate();
        }

        public override void WriteSettings()
        {
            Settings.Apply();
            Settings.Write();
            if (Settings.RequireReboot)
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PRF.Settings.RequireReboot".Translate(),
                    () => GenCommandLine.Restart()));
        }

        public static bool ShouldSuppressDisplace(IntVec3 cell, Map map, bool respawningAfterLoad)
        {
            return !respawningAfterLoad ||
                   map?.thingGrid.ThingsListAtFast(cell).OfType<Building_MassStorageUnit>().Any() != true;
        }
    }
}