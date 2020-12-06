using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompPowerWorkSetting : ThingComp, IPowerSupplyMachine
    {
        [Unsaved] private CompPowerTrader powerComp;


        private float powerForRange;

        private float powerForSpeed;

        public IRangeCells rangeCells;


        public IRangeCells[] rangeTypes = {new CircleRange(), new FacingRectRange(), new RectRange()};

        //Used for Saving the rangeCells . This is done as directly saving rangeCells leads to unknown Type Errors on Load
        private int rangeTypeSeletion = -1;
        public CompProperties_PowerWorkSetting Props => (CompProperties_PowerWorkSetting) props;

        private int rangeTypeSelection
        {
            get
            {
                if (rangeCells == null) rangeCells = (IRangeCells) Activator.CreateInstance(Props.rangeType);
                if (rangeCells.ToText() == new CircleRange().ToText()) return (int) rangeTypeClassEnum.CircleRange;
                if (rangeCells.ToText() == new FacingRectRange().ToText())
                    return (int) rangeTypeClassEnum.FacingRectRange;
                if (rangeCells.ToText() == new RectRange().ToText()) return (int) rangeTypeClassEnum.RectRange;
                return (int) rangeTypeClassEnum.RectRange;
            }

            set
            {
                if (value == (int) rangeTypeClassEnum.CircleRange) rangeCells = new CircleRange();
                if (value == (int) rangeTypeClassEnum.FacingRectRange) rangeCells = new FacingRectRange();
                if (value == (int) rangeTypeClassEnum.RectRange) rangeCells = new RectRange();
            }
        }

        public int MinPowerForSpeed => Props.minPowerForSpeed;

        public int MaxPowerForSpeed => Props.maxPowerForSpeed;

        public int MinPowerForRange => Props.minPowerForRange;

        public int MaxPowerForRange => Props.maxPowerForRange;

        public float SupplyPowerForSpeed
        {
            get => powerForSpeed;
            set
            {
                powerForSpeed = value;
                AdjustPower();
                RefreshPowerStatus();
            }
        }

        public float SupplyPowerForRange
        {
            get => powerForRange;
            set
            {
                powerForRange = value;
                AdjustPower();
                RefreshPowerStatus();
            }
        }

        public virtual bool Glowable => false;

        public virtual bool Glow
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool SpeedSetting => Props.speedSetting;

        public bool RangeSetting => Props.rangeSetting;

        public virtual float RangeInterval =>
            (Props.maxPowerForRange - Props.minPowerForRange) / (Props.maxRange - Props.minRange);

        public void RefreshPowerStatus()
        {
            if (powerComp != null)
                powerComp.PowerOutput =
                    -powerComp.Props.basePowerConsumption - SupplyPowerForSpeed - SupplyPowerForRange;
        }


        public override void PostExposeData()
        {
            base.PostExposeData();

            //Load the Current rangeCells Value
            if (Scribe.mode == LoadSaveMode.Saving) rangeTypeSeletion = rangeTypeSelection;

            Scribe_Values.Look(ref powerForSpeed, "powerForSpeed");
            Scribe_Values.Look(ref powerForRange, "powerForRange");
            Scribe_Values.Look(ref rangeTypeSeletion, "rangeType", -1);

            //Set the Loaded rangeCells Value
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (rangeTypeSeletion == -1)
                {
                    rangeCells = null;
                    rangeTypeSeletion = rangeTypeSelection;
                }

                rangeTypeSelection = rangeTypeSeletion;
            }

            AdjustPower();
            RefreshPowerStatus();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                powerForSpeed = Props.minPowerForSpeed;
                powerForRange = Props.minPowerForRange;
            }

            powerComp = parent.TryGetComp<CompPowerTrader>();
            AdjustPower();
            RefreshPowerStatus();
        }

        protected virtual void AdjustPower()
        {
            powerForSpeed = Mathf.Clamp(powerForSpeed, MinPowerForSpeed, MaxPowerForSpeed);

            powerForRange = Mathf.Clamp(powerForRange, MinPowerForRange, MaxPowerForRange);
        }

        public virtual float GetSpeedFactor()
        {
            var f = (powerForSpeed - MinPowerForSpeed) / (MaxPowerForSpeed - MinPowerForSpeed);
            return Mathf.Lerp(Props.minSpeedFactor, Props.maxSpeedFactor, f);
        }

        public virtual float GetRange()
        {
            if (RangeSetting)
            {
                var f = (powerForRange - MinPowerForRange) / (MaxPowerForRange - MinPowerForRange);
                return Mathf.Lerp(Props.minRange, Props.maxRange, f);
            }

            return 0f;
        }

        public virtual IEnumerable<IntVec3> GetRangeCells()
        {
            if (RangeSetting) return RangeCells(parent.Position, parent.Rotation, parent.def, GetRange());
            return null;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (RangeSetting) DrawRangeCells(Props.instance);
        }

        public virtual void DrawRangeCells(Color color)
        {
            var range = GetRange();
            GenDraw.DrawFieldEdges(GetRangeCells().ToList(), color);
        }


        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            if (rangeCells == null) rangeCells = (IRangeCells) Activator.CreateInstance(Props.rangeType);
            return rangeCells.RangeCells(center, rot, thingDef, range);
        }

        private enum rangeTypeClassEnum
        {
            CircleRange,
            FacingRectRange,
            RectRange
        }
    }

    public class CompProperties_PowerWorkSetting : CompProperties
    {
        public bool allowManualRangeTypeChange = false;
        public Color blueprintMax = Color.gray.A(0.6f);

        public Color blueprintMin = Color.white;
        public Color instance = Color.white;
        public int maxPowerForRange = 1000;
        public int maxPowerForSpeed = 0;
        public float maxRange = 6;
        public float maxSpeedFactor = 2;

        public int minPowerForRange = 0;
        public int minPowerForSpeed = 1000;

        public float minRange = 3;

        public float minSpeedFactor = 1;
        public Color otherInstance = Color.white.A(0.35f);
        public bool rangeSetting = false;

        public Type rangeType;

        public bool speedSetting = true;

        public CompProperties_PowerWorkSetting()
        {
            compClass = typeof(CompPowerWorkSetting);
        }

        private IRangeCells propsRangeType => (IRangeCells) Activator.CreateInstance(rangeType);

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol,
            AltitudeLayer drawAltitude, Thing thing = null)
        {
            if (rangeSetting)
            {
                base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
                var min = propsRangeType.RangeCells(center, rot, thingDef, minRange);
                var max = propsRangeType.RangeCells(center, rot, thingDef, maxRange);
                min.Select(c => new {Cell = c, Color = blueprintMin})
                    .Concat(max.Select(c => new {Cell = c, Color = blueprintMax}))
                    .GroupBy(a => a.Color)
                    .ToList()
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));

                var map = Find.CurrentMap;
                map.listerThings.ThingsOfDef(thingDef).Select(t => t.TryGetComp<CompPowerWorkSetting>())
                    .Where(c => c != null)
                    .ToList().ForEach(c => c.DrawRangeCells(otherInstance));
            }
        }
    }

    public interface IRangeCells
    {
        IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range);

        string ToText();
    }

    public class CircleRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            return GenRadial.RadialCellsAround(center, range + Mathf.Max(thingDef.size.x, thingDef.size.z) - 1, true);
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_CircleRange".Translate();
        }
    }

    public class FacingRectRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            return Util.FacingRect(center, thingDef.size, rot, Mathf.RoundToInt(range));
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_FacingRectRange".Translate();
        }
    }

    public class RectRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            var under = GenAdj.CellsOccupiedBy(center, rot, thingDef.size).ToHashSet();
            return GenAdj.CellsOccupiedBy(center, rot,
                    thingDef.size + new IntVec2(Mathf.RoundToInt(range) * 2, Mathf.RoundToInt(range) * 2))
                .Where(c => !under.Contains(c));
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_RectRange".Translate();
        }
    }
}