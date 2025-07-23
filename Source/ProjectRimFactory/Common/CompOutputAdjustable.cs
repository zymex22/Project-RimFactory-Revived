using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompOutputAdjustable : ThingComp
    {
        private int index = 0;
        
        // Normalized to possibleOutputs.Count
        private int Index
        {
            get => index;
            set
            {
                index = value;
                if (index < 0) index += possibleOutputs.Count;
                index %= possibleOutputs.Count;
            }
        }
        
        public bool Visible = true;

        public CompProperties_CompOutputAdjustable Props => (CompProperties_CompOutputAdjustable)this.props;

        private List<IntVec3> possibleOutputs = [];
        public IntVec3 CurrentCell => possibleOutputs[Index];

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            if (GravshipPlacementUtility.placingGravship)
            {
                var rotInCurrentLanding = Find.CurrentGravship.Rotation;
                var rotInt = rotInCurrentLanding.AsInt;
                rotInt = rotInt switch
                {
                    1 => 3,
                    3 => 1,
                    _ => rotInt
                };

                Index += ((possibleOutputs.Count / 4) * rotInt);
            }
            if (Props.SupportDiagonal)
            {
                possibleOutputs = [..GenAdj.CellsAdjacent8Way(parent)];
            }
            else
            {
                possibleOutputs = [..GenAdj.CellsAdjacentCardinal(parent)];
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Visible)
            {
                GenDraw.DrawFieldEdges([CurrentCell], Color.yellow);
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
                    actionL = () => Index++,
                    actionR = () => Index--,
                    icon = TexUI.RotRightTex,
                    defaultIconColor = Color.green
                };
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref index, "outputSlotIndex");
            Scribe_Values.Look(ref Visible, "Visible",true);
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
