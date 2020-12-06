﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjectRimFactory.Common;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Archo.Things
{
    public class Building_PortalGenerator : Building
    {
        public static readonly FieldInfo InnerContainerField =
            typeof(Building_Casket).GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance);

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Debug actions",
                    action = () => Find.WindowStack.Add(new FloatMenu(GetDebugActions()))
                };
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            PortalGeneratorUtility.DrawBlueprintFieldEdges(Position);
        }

        public List<FloatMenuOption> GetDebugActions()
        {
            return new List<FloatMenuOption>
            {
                new FloatMenuOption("Liquidate room", LiquidateRoom),
                new FloatMenuOption("Calculate eligibility for liquidation", () =>
                {
                    var acc = RecalculateEligibilityForTeleport();
                    if (acc.Accepted)
                        Messages.Message("Eligible for liquidation.", MessageTypeDefOf.PositiveEvent);
                    else
                        Messages.Message(acc.Reason, MessageTypeDefOf.RejectInput);
                })
            };
        }

        public AcceptanceReport RecalculateEligibilityForTeleport()
        {
            var humanPawnCount = 0;
            var room = Position.GetRoom(Map);
            if (room != null && room.OpenRoofCountStopAt(1) == 0)
            {
                // Check floors
                var floorPlan = PortalGeneratorUtility.FieldEdgeCells(Position);
                foreach (var cell in CellRect.CenteredOn(Position, 7, 7))
                {
                    foreach (var t in cell.GetThingList(Map))
                    {
                        if (t.def.passability == Traversability.Impassable)
                            return
                                "TeleportReport_CellImpassable"
                                    .Translate(); // Walls and other impassable buildings cannot be placed within a 7x7 square around portal.
                        if (t is Building_CryptosleepCasket c)
                            foreach (var thing in (IEnumerable<Thing>) InnerContainerField.GetValue(t))
                                if (thing is Pawn p)
                                    if (p.RaceProps.Humanlike)
                                        humanPawnCount++;
                    }

                    if (floorPlan[0].Contains(cell))
                    {
                        if (cell.GetTerrain(Map) != PRFDefOf.PRFFloorComputer)
                            return "TeleportReport_FloorLayoutIncorrect"
                                .Translate(); // The floor layout around the portal generator is incorrect.
                    }
                    else if (floorPlan[1].Contains(cell))
                    {
                        if (cell.GetTerrain(Map) != PRFDefOf.PRFZCompositeTile)
                            return "TeleportReport_FloorLayoutIncorrect"
                                .Translate(); // The floor layout around the portal generator is incorrect.
                    }
                    else if (floorPlan[2].Contains(cell))
                    {
                        if (cell.GetTerrain(Map) != PRFDefOf.PRFYCompositeTile)
                            return "TeleportReport_FloorLayoutIncorrect"
                                .Translate(); // The floor layout around the portal generator is incorrect.
                    }
                }
            }
            else
            {
                return "TeleportReport_RoomOutdoorsOrUnroofed".Translate(); // Room is outdoors or unroofed.
            }

            if (humanPawnCount == 0)
                return "TeleportReport_NoHumanlikePawnsForTransportation"
                    .Translate(); // No humanlike pawns to transport.
            return true;
        }

        public void LiquidateRoom()
        {
            var room = Position.GetRoom(Map, RegionType.Set_All);
            if (room != null && !room.PsychologicallyOutdoors)
            {
                var wealth = room.GetStat(RoomStatDefOf.Wealth);
                float roomSize = room.CellCount;
                float humanPawnCount = 0;
                float nonHumanPawnCount = 0;
                foreach (var cell in room.Cells)
                {
                    if (Map.terrainGrid.CanRemoveTopLayerAt(cell))
                    {
                        Map.terrainGrid.RemoveTopLayer(cell, false);
                        FilthMaker.RemoveAllFilth(cell, Map);
                    }

                    foreach (var t in cell.GetThingList(Map).ToList())
                    {
                        if (t is Building_CryptosleepCasket)
                            foreach (var thing in (IEnumerable<Thing>) InnerContainerField.GetValue(t))
                                if (thing is Pawn p)
                                {
                                    if (p.RaceProps.Humanlike)
                                        humanPawnCount++;
                                    else
                                        nonHumanPawnCount++;
                                }

                        if (t.def.destroyable && t != this) t.Destroy();
                    }
                }

                var points = 0.001f * wealth + roomSize + 10f * nonHumanPawnCount + 100f * humanPawnCount;
                if (Prefs.DevMode)
                    Log.Message(
                        $"==SpdTec Room Liquidation Report==\nWealth: {wealth}\nRoom size: {roomSize}\nPawns: (non-human {nonHumanPawnCount}), (human {humanPawnCount})\nPoints: {points}");
                Destroy();
            }
        }
    }
}