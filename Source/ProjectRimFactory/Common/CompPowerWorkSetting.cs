using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using RimWorld;

namespace ProjectRimFactory.Common
{
    public class CompPowerWorkSetting : ThingComp, IPowerSupplyMachine
    {
        public CompProperties_PowerWorkSetting Props => (CompProperties_PowerWorkSetting)this.props;

        public int MinPowerForSpeed => this.Props.minPowerForSpeed;

        public int MaxPowerForSpeed => this.Props.maxPowerForSpeed;

        public int MinPowerForRange => this.Props.minPowerForRange;

        public int MaxPowerForRange => this.Props.maxPowerForRange;

        public IRangeCells rangeCells = null;

        public float SupplyPowerForSpeed
        {
            get => this.powerForSpeed;
            set
            {
                this.powerForSpeed = value;
                this.AdjustPower();
                this.RefreshPowerStatus();
            }
        }

        public float SupplyPowerForRange
        {
            get => this.powerForRange;
            set
            {
                this.powerForRange = value;
                this.AdjustPower();
                this.RefreshPowerStatus();
            }
        }

        public virtual bool Glowable => false;

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual bool SpeedSetting => this.Props.speedSetting;

        public bool RangeSetting => this.Props.rangeSetting;

        public virtual float RangeInterval => (this.Props.maxPowerForRange - this.Props.minPowerForRange) / (this.Props.maxRange - this.Props.minRange);

        private float powerForSpeed = 0;

        
        private float powerForRange = 0;

        private enum rangeTypeClassEnum{
            CircleRange,
            FacingRectRange,
            RectRange
        }

        private int rangeTypeSelection
        {
            get
            {
                if (rangeCells == null) rangeCells = (IRangeCells)Activator.CreateInstance(Props.rangeType);
                if (rangeCells.ToText() == new CircleRange().ToText()) return (int)rangeTypeClassEnum.CircleRange;
                if (rangeCells.ToText() == new FacingRectRange().ToText()) return (int)rangeTypeClassEnum.FacingRectRange;
                if (rangeCells.ToText() == new RectRange().ToText()) return (int)rangeTypeClassEnum.RectRange;
                return (int)rangeTypeClassEnum.RectRange;
            }

            set
            {
                if (value == (int)rangeTypeClassEnum.CircleRange) rangeCells = new CircleRange();
                if (value == (int)rangeTypeClassEnum.FacingRectRange) rangeCells = new FacingRectRange();
                if (value == (int)rangeTypeClassEnum.RectRange) rangeCells = new RectRange();
            }

        }

        //Used for Saving the rangeCells . This is done as directly saving rangeCells leads to unknown Type Errors on Load
        private int rangeTypeSeletion = -1;


        public override void PostExposeData()
        {
            base.PostExposeData();

            //Load the Current rangeCells Value
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                rangeTypeSeletion = rangeTypeSelection;
            }

            Scribe_Values.Look<float>(ref this.powerForSpeed, "powerForSpeed");
            Scribe_Values.Look<float>(ref this.powerForRange, "powerForRange");
            Scribe_Values.Look(ref rangeTypeSeletion, "rangeType",-1);

            //Set the Loaded rangeCells Value
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (rangeTypeSeletion == -1) {
                    rangeCells = null;
                    rangeTypeSeletion = rangeTypeSelection;
                } 

                rangeTypeSelection = rangeTypeSeletion;
            }

            this.AdjustPower();
            this.RefreshPowerStatus();
        }

        [Unsaved]
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.powerForSpeed = this.Props.minPowerForSpeed;
                this.powerForRange = this.Props.minPowerForRange;
            }
            this.powerComp = this.parent.TryGetComp<CompPowerTrader>();
            this.AdjustPower();
            this.RefreshPowerStatus();
        }

        protected virtual void AdjustPower()
        {
            if (this.powerForSpeed < this.MinPowerForSpeed)
            {
                this.powerForSpeed = this.MinPowerForSpeed;
            }
            if (this.powerForSpeed > this.MaxPowerForSpeed)
            {
                this.powerForSpeed = this.MaxPowerForSpeed;
            }
            if (this.powerForRange < this.MinPowerForRange)
            {
                this.powerForRange = this.MinPowerForRange;
            }
            if (this.powerForRange > this.MaxPowerForRange)
            {
                this.powerForRange = this.MaxPowerForRange;
            }
        }

        public void RefreshPowerStatus()
        {
            if(this.powerComp != null)
            {
                this.powerComp.PowerOutput = -this.powerComp.Props.basePowerConsumption - this.SupplyPowerForSpeed - this.SupplyPowerForRange;
            }
        }

        public virtual float GetSpeedFactor()
        {
            var f = (this.powerForSpeed - this.MinPowerForSpeed) / (this.MaxPowerForSpeed - this.MinPowerForSpeed);
            return Mathf.Lerp(this.Props.minSpeedFactor, this.Props.maxSpeedFactor, f);
        }

        public virtual float GetRange()
        {
            if (this.RangeSetting)
            {
                var f = (this.powerForRange - this.MinPowerForRange) / (this.MaxPowerForRange - this.MinPowerForRange);
                return Mathf.Lerp(this.Props.minRange, this.Props.maxRange, f);
            }
            return 0f;
        }

        public virtual IEnumerable<IntVec3> GetRangeCells()
        {
            if (this.RangeSetting)
            {
                return this.RangeCells(this.parent.Position, this.parent.Rotation, this.parent.def, this.GetRange());
            }
            return null;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (this.RangeSetting)
            {
                this.DrawRangeCells(this.Props.instance);
            }
        }

        public virtual void DrawRangeCells(Color color)
        {
            var range = GetRange();
            GenDraw.DrawFieldEdges(this.GetRangeCells().ToList(), color);
        }


        

        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            if (this.rangeCells == null)
            {
                this.rangeCells = (IRangeCells)Activator.CreateInstance(Props.rangeType);
            }
            return (this.rangeCells as IRangeCells).RangeCells(center, rot, thingDef, range);
        }




        public IRangeCells[] rangeTypes = new IRangeCells[] { new  CircleRange() , new FacingRectRange() , new RectRange() }; 








    }

    public class CompProperties_PowerWorkSetting : CompProperties
    {
        public int maxPowerForSpeed = 0;
        public int minPowerForSpeed = 1000;

        public float minSpeedFactor = 1;
        public float maxSpeedFactor = 2;

        public int minPowerForRange = 0;
        public int maxPowerForRange = 1000;

        public float minRange = 3;
        public float maxRange = 6;

        public bool speedSetting = true;
        public bool rangeSetting = false;

        public bool allowManualRangeTypeChange = false;

        public Color blueprintMin = Color.white;
        public Color blueprintMax = Color.gray.A(0.6f);
        public Color instance = Color.white;
        public Color otherInstance = Color.white.A(0.35f);

        public Type rangeType;

        private IRangeCells propsRangeType => (IRangeCells)Activator.CreateInstance(rangeType);

        public CompProperties_PowerWorkSetting()
        {
            this.compClass = typeof(CompPowerWorkSetting);
        }

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
        {
            if (this.rangeSetting)
            {
                base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
                var min = propsRangeType.RangeCells(center, rot, thingDef, this.minRange);
                var max = propsRangeType.RangeCells(center, rot, thingDef, this.maxRange);
                min.Select(c => new { Cell = c, Color = this.blueprintMin })
                    .Concat(max.Select(c => new { Cell = c, Color = this.blueprintMax }))
                    .GroupBy(a => a.Color)
                    .ToList()
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));

                Map map = Find.CurrentMap;
                map.listerThings.ThingsOfDef(thingDef).Select(t => t.TryGetComp<CompPowerWorkSetting>()).Where(c => c != null)
                    .ToList().ForEach(c => c.DrawRangeCells(this.otherInstance));
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
            return GenRadial.RadialCellsAround(center, range, true);
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
            return GenAdj.CellsOccupiedBy(center, rot, thingDef.size + new IntVec2(Mathf.RoundToInt(range) * 2, Mathf.RoundToInt(range) * 2))
                .Where(c => !under.Contains(c));
        }
        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_RectRange".Translate();
        }
    }
}
