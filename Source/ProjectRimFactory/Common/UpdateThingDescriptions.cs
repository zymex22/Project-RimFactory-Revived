using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common
{

    public interface IXMLThingDescription
    {
        public string GetDescription(ThingDef def);
    }


    public static class UpdateThingDescriptions
    {

        //Ensurs that UpdateThingDescriptions() is only run once.
        private static bool updatedThingDescriptions = false;

        private static void UpdateMines()
        {
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefs.Where(d => d.thingClass == typeof(Building_WorkTable) && d.HasModExtension<ModExtension_ModifyProduct>()).ToList();
            foreach (ThingDef thing in thingDefs)
            {
                if (thing is null || thing.recipes is null) continue;
                
                string HelpText = "\r\n\r\n";

                HelpText += "PRF_DescriptionUpdate_CanMine".Translate();
                foreach (RecipeDef recipeDef in thing.recipes)
                {
                    ThingDefCountClass prouct = recipeDef.products?[0];
                    HelpText += String.Format("    - {0}\r\n", prouct?.Label);
                }      

                thing.description += HelpText;            
            }
        }


        private static void UpdateInterface()
        {
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefs.ToList();
            foreach (ThingDef thing in thingDefs)
            {
                if (thing is null) continue;
                var comps = thing.comps?.Where(c => (c as IXMLThingDescription) != null).ToList();
                var modext = thing.modExtensions?.Where(m => (m as IXMLThingDescription) != null).ToList();

                var inter = thing.thingClass.GetInterface(nameof(IXMLThingDescription));

                string HelpText = "\r\n\r\n";
                if (inter != null)
                {
                    HelpText += ((IXMLThingDescription)Activator.CreateInstance(thing.thingClass)).GetDescription(thing);
                }

               // HelpText += thingdesc?.GetDescription() ?? "";
                //May need to change the order later

                if (comps is not null)
                {
                    foreach (var comp in comps)
                    {
                        HelpText += (comp as IXMLThingDescription).GetDescription(thing);
                    }
                }
                if (modext is not null)
                {
                    foreach (var ext in modext)
                    {
                        HelpText += (ext as IXMLThingDescription).GetDescription(thing);
                    }
                }
                if (HelpText != "\r\n\r\n") thing.description += HelpText;
            }
        }



        //It needs to run after all [StaticConstructorOnStartup] have been called 
        public static void Update()
        {
            if (updatedThingDescriptions) return;
            updatedThingDescriptions = true;

            //Updates the description of Things with ModExtension_ModifyProduct & ModExtension_Miner
            UpdateMines();


            UpdateInterface();



        }
    }
}
