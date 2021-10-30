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

        public bool Visible = true;

        public CompProperties_CompOutputAdjustable Props => (CompProperties_CompOutputAdjustable)this.props;

        List<IntVec3> possibleOutputs = new List<IntVec3>();
        public IntVec3 CurrentCell
        {
            get
            {
                if (possibleOutputs.Count == 0) initPossibleOutputs(); //This is impossible, but there is one report (one-off) of it.
                return possibleOutputs[index %= possibleOutputs.Count];
            }
        }

        private void initPossibleOutputs()
        {
            if (Props.SupportDiagonal)
            {
                possibleOutputs = GenAdj.CellsAdjacent8Way(parent).ToList();
            }
            else
            {
                possibleOutputs = GenAdj.CellsAdjacentCardinal(parent).ToList();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            initPossibleOutputs();
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Visible)
            {
                GenDraw.DrawFieldEdges(new List<IntVec3> { CurrentCell }, Color.yellow);
            }
            
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            if (Visible)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "AdjustDirection_Output".Translate(),
                    action = () => index++,
                    icon = TexUI.RotRightTex,
                    defaultIconColor = Color.green
                };
            }
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
            this.compClass = typeof(CompOutputAdjustable);
        }

    }
}
