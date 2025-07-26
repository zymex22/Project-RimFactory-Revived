using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    // ReSharper disable once UnusedType.Global
    public class Building_IOPusher : Building_StorageUnitIOBase
    {
        protected override IntVec3 WorkPosition => Position + Rotation.FacingCell;


        public override StorageIOMode IOMode { get => StorageIOMode.Output; set => _ = value; }

        protected override bool IsAdvancedPort => false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Mode = IOMode;
        }
    }

    // ReSharper disable once UnusedType.Global
    class PlaceWorker_IOPusherHilight : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            // base.DrawGhost(def, center, rot, ghostCol, thing);

            IntVec3 outputCell = center + rot.FacingCell;


            GenDraw.DrawFieldEdges([outputCell], Common.CommonColors.outputCell);



        }
    }



}
