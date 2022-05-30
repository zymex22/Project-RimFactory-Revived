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

        public int MaxPowerForSpeed =>  (int)(this.Props.floatrange_SpeedFactor.Span * this.Props.powerPerStepSpeed);

        public int MaxPowerForRange =>  (int)(this.Props.floatrange_Range.Span * this.Props.powerPerStepRange);

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

        public virtual bool SpeedSetting
        {
            get
            {
                if (SpeedSettingHide) return false;
                return this.Props.floatrange_SpeedFactor.Span > 0;
            }
        }

        public bool SpeedSettingHide = false;

        public bool RangeSettingHide = false;

        public bool RangeSetting 
        {
            get
            {
                if (RangeSettingHide) return false;
                return this.Props.floatrange_Range.Span > 0;
            }   
              
        }

        private float powerForSpeed = 0;


        public Rot4 RangeTypeRot = Rot4.North;

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

        public int BasePowerConsumption => (int)this.powerComp.Props.basePowerConsumption;

        public int CurrentPowerConsumption => (int)this.powerComp.PowerOutput;

        public Dictionary<string, int> AdditionalPowerConsumption => (this.parent as IAdditionalPowerConsumption)?.AdditionalPowerConsumption ?? null;

        private int AdditionalPowerDrain
        {
            get
            {
                if (AdditionalPowerConsumption != null && AdditionalPowerConsumption.Count > 0)
                {

                    return AdditionalPowerConsumption.Values.ToList().Sum();
                }
                else
                {
                    return 0;
                }
            }
        }

        public float PowerPerStepSpeed => this.Props.powerPerStepSpeed / this.Props.powerStepFactor;

        public float PowerPerStepRange => this.Props.powerPerStepRange;

        public FloatRange FloatRange_Range => this.Props.floatrange_Range;

        public float CurrentRange => this.GetRange();

        public FloatRange FloatRange_SpeedFactor => this.Props.floatrange_SpeedFactor;

        public float CurrentSpeedFactor => this.GetSpeedFactor();

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
            Scribe_Values.Look(ref RangeTypeRot, "RangeTypeRot",Rot4.North);

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
                this.powerForSpeed = 0;
                this.powerForRange = 0;
            }
            this.powerComp = this.parent.TryGetComp<CompPowerTrader>();
            this.AdjustPower();
            this.RefreshPowerStatus();
        }

        protected virtual void AdjustPower()
        {
            this.powerForSpeed = Mathf.Clamp(this.powerForSpeed, 0, this.MaxPowerForSpeed);

            this.powerForRange = Mathf.Clamp(this.powerForRange, 0, this.MaxPowerForRange);
        }

        public void RefreshPowerStatus()
        {
            if(this.powerComp != null)
            {
                this.powerComp.PowerOutput = -this.powerComp.Props.basePowerConsumption - this.SupplyPowerForSpeed - this.SupplyPowerForRange - AdditionalPowerDrain;
            }
        }

        public virtual float GetSpeedFactor()
        {
            var f = 0f;
            if (this.MaxPowerForSpeed != 0)
            {
                f = (this.powerForSpeed) / (this.MaxPowerForSpeed);
            }
            return Mathf.Lerp(this.Props.floatrange_SpeedFactor.min, this.Props.floatrange_SpeedFactor.max, f);
        }

        public virtual float GetRange()
        {
            if (this.RangeSetting)
            {
                var f = (this.powerForRange) / (this.MaxPowerForRange);
                return Mathf.Lerp(this.Props.floatrange_Range.min, this.Props.floatrange_Range.max, f);
            }
            return 0f;
        }

        public virtual IEnumerable<IntVec3> GetRangeCells()
        {
            if (this.RangeSetting)
            {
                //While adding a inBounds Check here might seem like a good idea doing so creates a risk to miss bugs.
                //We currently use many base Game functions as a fallback. those do not check for Bounds.
                //We should consider to implement a Class dedicated to this Job (When his is done we shall reconsider each check made in this commit / #314)
                return this.RangeCells(this.parent.Position, RangeTypeRot, this.parent.def, this.GetRange())/*.Where(c => c.InBounds(this.parent.Map))*/;
            }
            return null;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (this.RangeSetting)
            {
                this.DrawRangeCells(CommonColors.instance);
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

    public class CompProperties_PowerWorkSetting : CompProperties , IXMLThingDescription
    {
        //speed
        public FloatRange floatrange_SpeedFactor;
        public float powerPerStepSpeed;
        public float powerStepFactor = 1;

        //Range
        public FloatRange floatrange_Range;
        public float powerPerStepRange;

        //Range Type Settings
        public bool allowManualRangeTypeChange = false;
        public Type rangeType;



        private IRangeCells propsRangeType => (IRangeCells)Activator.CreateInstance(rangeType);

        public CompProperties_PowerWorkSetting()
        {
            this.compClass = typeof(CompPowerWorkSetting);
        }

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
        {
            if (this.floatrange_Range.Span > 0)
            {
                base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
                var min = propsRangeType.RangeCells(center, rot, thingDef, this.floatrange_Range.min);
                var max = propsRangeType.RangeCells(center, rot, thingDef, this.floatrange_Range.max);
                min.Select(c => new { Cell = c, Color = CommonColors.blueprintMin })
                    .Concat(max.Select(c => new { Cell = c, Color = CommonColors.blueprintMax }))
                    .GroupBy(a => a.Color)
                    .ToList()
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));

                Map map = Find.CurrentMap;
                map.listerThings.ThingsOfDef(thingDef).Select(t => t.TryGetComp<CompPowerWorkSetting>()).Where(c => c != null && c.RangeSetting)
                    .ToList().ForEach(c => c.DrawRangeCells(CommonColors.otherInstance));
            }
        }

        //https://stackoverflow.com/a/457708
        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public string GetDescription(ThingDef def)
        {
            string helptext = "";
            string tempstr;


            bool isOfTypeBuilding_BaseMachine = IsSubclassOfRawGeneric(typeof(AutoMachineTool.Building_Base<>), def.thingClass);
            int factor = isOfTypeBuilding_BaseMachine ? 10 : 1;


            if (floatrange_SpeedFactor.Span > 0)
            {
                tempstr = $"{floatrange_SpeedFactor.min * factor} - {floatrange_SpeedFactor.max * factor}";
            }
            else
            {
                tempstr = $"{floatrange_SpeedFactor.min * factor}";
            }
            helptext += "PRF_UTD_CompProperties_PowerWorkSetting_Speed".Translate(tempstr);
            helptext += "\r\n";
            if (floatrange_Range.Span > 0)
            {
                tempstr = $"{floatrange_Range.min} - {floatrange_Range.max}";
            }
            else
            {
                tempstr = $"{floatrange_Range.min}";
            }
            helptext += "PRF_UTD_CompProperties_PowerWorkSetting_Range".Translate(tempstr);
            helptext += "\r\n";
            helptext += "PRF_UTD_CompProperties_PowerWorkSetting_RangeType".Translate(propsRangeType.ToText());
            helptext += "\r\n";
            if (allowManualRangeTypeChange) helptext += "PRF_UTD_CompProperties_PowerWorkSetting_RangeTypeChange".Translate() + "\r\n"; 
            return helptext; 
        }
    }

    public interface IRangeCells
    {
        IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range);

        string ToText();

        bool NeedsRotate { get; }

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

        public bool NeedsRotate => false;
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

        public bool NeedsRotate => true;
    }

    public class RectRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            IntVec2 size = thingDef.size;
            Util.CounterAdjustForRotation(ref center,ref size, rot);


            var under = GenAdj.CellsOccupiedBy(center, rot, size).ToHashSet();
            return GenAdj.CellsOccupiedBy(center, rot, thingDef.size + new IntVec2(Mathf.RoundToInt(range) * 2, Mathf.RoundToInt(range) * 2))
                .Where(c => !under.Contains(c));
        }
        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_RectRange".Translate();
        }

        public bool NeedsRotate => false;
    }
}
