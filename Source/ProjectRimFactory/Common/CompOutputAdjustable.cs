using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompOutputAdjustable : ThingComp
    {
        private int index;

        private List<IntVec3> possibleOutputs = new List<IntVec3>();

        public CompProperties_CompOutputAdjustable Props => (CompProperties_CompOutputAdjustable) props;

        public IntVec3 CurrentCell => possibleOutputs[index %= possibleOutputs.Count];

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.SupportDiagonal)
                possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacent8Way(parent));
            else
                possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacentCardinal(parent));
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3> {CurrentCell}, Color.yellow);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            yield return new Command_Action
            {
                defaultLabel = "AdjustDirection_Output".Translate(),
                action = () => index++,
                icon = TexUI.RotRightTex,
                defaultIconColor = Color.green
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref index, "outputSlotIndex");
        }
    }

    public class CompProperties_CompOutputAdjustable : CompProperties
    {
        public bool SupportDiagonal = false;

        public CompProperties_CompOutputAdjustable()
        {
            compClass = typeof(CompOutputAdjustable);
        }
    }
}