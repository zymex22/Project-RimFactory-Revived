using ProjectRimFactory.SAL3;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectRimFactory.SAL3.Tools;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompPowerWorkSetting : ThingComp, IPowerSupplyMachine
    {
        public CompProperties_PowerWorkSetting Props => (CompProperties_PowerWorkSetting)props;

        public int MaxPowerForSpeed => (int)(Props.floatrange_SpeedFactor.Span * Props.powerPerStepSpeed);

        public int MaxPowerForRange => (int)(Props.floatrange_Range.Span * Props.powerPerStepRange);

        public IRangeCells rangeCells;

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

        public virtual bool Glow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual bool SpeedSetting
        {
            get
            {
                if (SpeedSettingHide) return false;
                return Props.floatrange_SpeedFactor.Span > 0;
            }
        }

        public bool SpeedSettingHide = false;

        public bool RangeSettingHide = false;

        public bool RangeSetting
        {
            get
            {
                if (RangeSettingHide) return false;
                return Props.floatrange_Range.Span > 0;
            }

        }

        private float powerForSpeed;


        public Rot4 RangeTypeRot = Rot4.North;

        private float powerForRange;

        private enum rangeTypeClassEnum
        {
            CircleRange,
            FacingRectRange,
            RectRange
        }

        private int rangeTypeSelection
        {
            get
            {
                rangeCells ??= (IRangeCells)Activator.CreateInstance(Props.rangeType);
                if (rangeCells.ToText() == new CircleRange().ToText()) return (int)rangeTypeClassEnum.CircleRange;
                if (rangeCells.ToText() == new FacingRectRange().ToText()) return (int)rangeTypeClassEnum.FacingRectRange;
                if (rangeCells.ToText() == new RectRange().ToText()) return (int)rangeTypeClassEnum.RectRange;
                return (int)rangeTypeClassEnum.RectRange;
            }

            set
            {
                rangeCells = value switch
                {
                    (int)rangeTypeClassEnum.CircleRange => new CircleRange(),
                    (int)rangeTypeClassEnum.FacingRectRange => new FacingRectRange(),
                    (int)rangeTypeClassEnum.RectRange => new RectRange(),
                    _ => rangeCells
                };
            }

        }

        public float BasePowerConsumption => (float)ReflectionUtility.CompPropertiesPowerBasePowerConsumption.GetValue(powerComp.Props);

        public int CurrentPowerConsumption => (int)powerComp.PowerOutput;

        public Dictionary<string, int> AdditionalPowerConsumption => (parent as IAdditionalPowerConsumption)?.AdditionalPowerConsumption ?? null;

        private int AdditionalPowerDrain
        {
            get
            {
                if (AdditionalPowerConsumption is { Count: > 0 })
                {
                    return AdditionalPowerConsumption.Values.ToList().Sum();
                }
                return 0;
            }
        }

        public float PowerPerStepSpeed => Props.powerPerStepSpeed / Props.powerStepFactor;

        public float PowerPerStepRange => Props.powerPerStepRange;

        public FloatRange FloatRangeRange => Props.floatrange_Range;

        public float CurrentRange => GetRange();

        public FloatRange FloatRangeSpeedFactor => Props.floatrange_SpeedFactor;

        public float CurrentSpeedFactor => GetSpeedFactor();

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

            Scribe_Values.Look(ref RangeSettingHide, "RangeSettingHide",false);
            Scribe_Values.Look(ref powerForSpeed, "powerForSpeed");
            Scribe_Values.Look(ref powerForRange, "powerForRange");
            Scribe_Values.Look(ref rangeTypeSeletion, "rangeType", -1);
            Scribe_Values.Look(ref RangeTypeRot, "RangeTypeRot", Rot4.North);

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

        [Unsaved]
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            // TODO: in 1.6 that activated on GravShip move, that should not happen
            // That's why I commented it out
            // I fear there was a reason for this but i can't find any
            /*
            if (!respawningAfterLoad)
            {
                powerForSpeed = 0;
                powerForRange = 0;
            }*/
            powerComp = parent.TryGetComp<CompPowerTrader>();
            AdjustPower();
            RefreshPowerStatus();
        }

        protected virtual void AdjustPower()
        {
            powerForSpeed = Mathf.Clamp(powerForSpeed, 0, MaxPowerForSpeed);

            powerForRange = Mathf.Clamp(powerForRange, 0, MaxPowerForRange);
        }

        public void RefreshPowerStatus()
        {
            if (powerComp != null)
            {
                powerComp.PowerOutput = -(float)ReflectionUtility.CompPropertiesPowerBasePowerConsumption.GetValue(powerComp.Props) - SupplyPowerForSpeed - SupplyPowerForRange - AdditionalPowerDrain;
            }
        }

        public virtual float GetSpeedFactor()
        {
            var f = 0f;
            if (MaxPowerForSpeed != 0)
            {
                f = (powerForSpeed) / (MaxPowerForSpeed);
            }
            return Mathf.Lerp(Props.floatrange_SpeedFactor.min, Props.floatrange_SpeedFactor.max, f);
        }

        public virtual float GetRange()
        {
            if (!RangeSetting) return 0f;
            var f = (powerForRange) / (MaxPowerForRange);
            return Mathf.Lerp(Props.floatrange_Range.min, Props.floatrange_Range.max, f);
        }

        public virtual IEnumerable<IntVec3> GetRangeCells()
        {
            if (!RangeSetting) return null;
            //While adding a inBounds Check here might seem like a good idea doing so creates a risk to miss bugs.
            //We currently use many base Game functions as a fallback. those do not check for Bounds.
            //We should consider to implement a Class dedicated to this Job (When his is done we shall reconsider each check made in this commit / #314)
            return RangeCells(parent.Position, RangeTypeRot, parent.def, GetRange())/*.Where(c => c.InBounds(this.parent.Map))*/;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (RangeSetting)
            {
                DrawRangeCells(CommonColors.Instance);
            }
        }

        public virtual void DrawRangeCells(Color color)
        {
            var _ = GetRange();
            GenDraw.DrawFieldEdges(GetRangeCells().ToList(), color);
        }




        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            rangeCells ??= (IRangeCells)Activator.CreateInstance(Props.rangeType);
            return rangeCells.RangeCells(center, rot, thingDef, range);
        }




        public IRangeCells[] rangeTypes = { new CircleRange(), new FacingRectRange(), new RectRange() };

    }

    public class CompProperties_PowerWorkSetting : CompProperties, IXMLThingDescription
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
            compClass = typeof(CompPowerWorkSetting);
        }

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
        {
            if (!(floatrange_Range.Span > 0)) return;
            base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
            var min = propsRangeType.RangeCells(center, rot, thingDef, floatrange_Range.min);
            var max = propsRangeType.RangeCells(center, rot, thingDef, floatrange_Range.max);
            min.Select(c => new { Cell = c, Color = CommonColors.BlueprintMin })
                .Concat(max.Select(c => new { Cell = c, Color = CommonColors.BlueprintMax }))
                .GroupBy(a => a.Color)
                .ToList()
                .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));

            var map = Find.CurrentMap;
            map.listerThings.ThingsOfDef(thingDef).Select(t => t.TryGetComp<CompPowerWorkSetting>())
                .Where(c => c is { RangeSetting: true })
                .ToList().ForEach(c => c.DrawRangeCells(CommonColors.OtherInstance));
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
            var helpText = string.Empty;
            
            var factor = IsSubclassOfRawGeneric(typeof(AutoMachineTool.Building_Base<>), def.thingClass) ? 10 : 1;

            var tempString = floatrange_SpeedFactor.Span > 0 ?
                $"{floatrange_SpeedFactor.min * factor} - {floatrange_SpeedFactor.max * factor}" :
                $"{floatrange_SpeedFactor.min * factor}";
            //Single speed of 1 is not intersting
            if (tempString != "1")
            {
                helpText += "PRF_UTD_CompProperties_PowerWorkSetting_Speed".Translate(tempString);
                helpText += "\r\n";
            }
            tempString = floatrange_Range.Span > 0 ?
                $"{floatrange_Range.min} - {floatrange_Range.max}" :
                $"{floatrange_Range.min}";
            
            //static range of 1 or 0 is not relevant for display
            if (tempString != "0" && tempString != "1")
            {
                helpText += "PRF_UTD_CompProperties_PowerWorkSetting_Range".Translate(tempString);
                helpText += "\r\n";

                helpText += "PRF_UTD_CompProperties_PowerWorkSetting_RangeType".Translate(propsRangeType.ToText());
                helpText += "\r\n";
            }

            if (allowManualRangeTypeChange) helpText += "PRF_UTD_CompProperties_PowerWorkSetting_RangeTypeChange".Translate() + "\r\n";
            return helpText;
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
            Util.CounterAdjustForRotation(ref center, ref size, rot);


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
