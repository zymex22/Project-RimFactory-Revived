using UnityEngine;
using Verse;
using System.IO;
using System.Reflection;

namespace ProjectRimFactory.CultivatorTools
{
    public class CTModSettings : ModSettings
    {
        public bool FancyGraphics = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref FancyGraphics, "FancyGraphics", true);
        }

        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard()
            {
                ColumnWidth = inRect.width
            };
            list.Begin(inRect);
            list.CheckboxLabeled("CultivatorToolsSettings_FancyGraphics".Translate(), ref FancyGraphics, "CultivatorToolsSettings_FancyGraphicsDesc".Translate());
            list.End();
        }

        public void WriteSettings(CTMod instance) => LoadedModManager.WriteModSettings(instance.Content.Identifier, instance.GetType().Name, this);
    }
    public class CTMod : Mod
    {
        public static CTModSettings settings = new CTModSettings();

        public CTMod(ModContentPack content) : base(content)
        {
            string path = (string)typeof(LoadedModManager).GetMethod("GetSettingsFilename", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { Content.Identifier, GetType().Name });
            if (File.Exists(path))
                settings = GetSettings<CTModSettings>();
        }

        public override void WriteSettings() => settings.WriteSettings(this);

        public override string SettingsCategory() => "SS Cultivator Tools";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
