using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class CompRecipeImportRange : ThingComp
    {
        public CompProperties_RecipeImportRange Props => (CompProperties_RecipeImportRange)props;

        public IEnumerable<IntVec3> RangeCells()
        {
            return Props.CellsWithinRange(parent.Position);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(Props.CellsWithinRange(parent.Position).ToList(), Props.ghostColor);
        }

    }

    public class CompProperties_RecipeImportRange : CompProperties, Common.IXMLThingDescription
    {
        public CompProperties_RecipeImportRange()
        {
            compClass = typeof(CompRecipeImportRange);
        }

        public float range = 5f;

        public Color ghostColor = Color.blue;

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
        {
            base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
            var list = CellsWithinRange(center).ToList();
            GenDraw.DrawFieldEdges(list, ghostColor);
        }

        public IEnumerable<IntVec3> CellsWithinRange(IntVec3 center)
        {
            return GenRadial.RadialCellsAround(center, range, true);
        }

        public string GetDescription(ThingDef def)
        {
            string text = "PRF_UTD_CompProperties_RecipeImportRange_Range".Translate(range) + "\r\n";

            return text;
        }
    }
}
