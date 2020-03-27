using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompOutputAdjustable : ThingComp
    {
        int index;
        List<IntVec3> possibleOutputs = new List<IntVec3>();
        public IntVec3 CurrentCell
        {
            get
            {
                return possibleOutputs[index %= possibleOutputs.Count];
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacentCardinal(parent));
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3> { CurrentCell }, Color.yellow);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            yield return new Command_Action()
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
}
