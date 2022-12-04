using System.Collections.Generic;
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
                return possibleOutputs[index %= possibleOutputs.Count];
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.SupportDiagonal)
            {
                possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacent8Way(parent));
            }
            else
            {
                possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacentCardinal(parent));
            }

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
                yield return new Command_ActionRightLeft()
                {
                    defaultLabel = "AdjustDirection_Output".Translate(),
                    actionL = () => index++,
                    actionR = () =>
                    {
                        if (index == 0)
                        {
                            index = possibleOutputs.Count - 1;
                        }
                        else
                        {
                            index--;
                        }
                    },
                    icon = TexUI.RotRightTex,
                    defaultIconColor = Color.green
                };
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref index, "outputSlotIndex");
            Scribe_Values.Look(ref Visible, "Visible");
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
