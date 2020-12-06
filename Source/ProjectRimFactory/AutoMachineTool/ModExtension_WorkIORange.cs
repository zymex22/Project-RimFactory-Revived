using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class ModExtension_WorkIORange : DefModExtension
    {
        public Color blueprintMax = Color.gray.A(0.6f);

        public Color blueprintMin = Color.white;
        public Color inputCell = Color.white;
        private IInputCellResolver inputCellResolver;

        public Type inputCellResolverType;
        public Color inputZone = Color.white.A(0.5f);
        public Color instance = Color.white;
        public int maxPower = 0;
        public int minPower = 0;
        public Color otherInstance = Color.white.A(0.35f);
        public Color outputCell = Color.yellow;
        private IOutputCellResolver outputCellResolver;

        public Type outputCellResolverType;
        public Color outputZone = Color.yellow.A(0.5f);
        private ITargetCellResolver targetCellResolver;

        public Type targetCellResolverType;

        public ITargetCellResolver TargetCellResolver
        {
            get
            {
                if (targetCellResolverType == null) return null;
                if (targetCellResolver == null)
                {
                    targetCellResolver = (ITargetCellResolver) Activator.CreateInstance(targetCellResolverType);
                    targetCellResolver.Parent = this;
                }

                return targetCellResolver;
            }
        }

        public IOutputCellResolver OutputCellResolver
        {
            get
            {
                if (outputCellResolverType == null) return null;
                if (outputCellResolver == null)
                {
                    outputCellResolver = (IOutputCellResolver) Activator.CreateInstance(outputCellResolverType);
                    outputCellResolver.Parent = this;
                }

                return outputCellResolver;
            }
        }

        public IInputCellResolver InputCellResolver
        {
            get
            {
                if (inputCellResolverType == null) return null;
                if (inputCellResolver == null)
                {
                    inputCellResolver = (IInputCellResolver) Activator.CreateInstance(inputCellResolverType);
                    inputCellResolver.Parent = this;
                }

                return inputCellResolver;
            }
        }

        public Color GetCellPatternColor(CellPattern pat)
        {
            switch (pat)
            {
                case CellPattern.BlurprintMin:
                    return blueprintMin;
                case CellPattern.BlurprintMax:
                    return blueprintMax;
                case CellPattern.Instance:
                    return instance;
                case CellPattern.OtherInstance:
                    return otherInstance;
                case CellPattern.OutputCell:
                    return outputCell;
                case CellPattern.OutputZone:
                    return outputZone;
                case CellPattern.InputCell:
                    return inputCell;
                case CellPattern.InputZone:
                    return inputZone;
            }

            return Color.white;
        }
    }

    public interface IInputCellResolver
    {
        ModExtension_WorkIORange Parent { get; set; }
        Option<IntVec3> InputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot);
        IEnumerable<IntVec3> InputZoneCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot);
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
    }

    public interface IOutputCellResolver
    {
        ModExtension_WorkIORange Parent { get; set; }
        Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot);
        IEnumerable<IntVec3> OutputZoneCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot);
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
    }

    public class OutputCellResolver : IOutputCellResolver
    {
        private static readonly List<IntVec3> EmptyList = new List<IntVec3>();
        public ModExtension_WorkIORange Parent { get; set; }

        public virtual Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return Option(FacingCell(center, size, rot.Opposite));
        }

        public virtual IEnumerable<IntVec3> OutputZoneCells(ThingDef def, IntVec3 center, IntVec2 size, Map map,
            Rot4 rot)
        {
            return OutputCell(def, center, size, map, rot).Select(c => c.SlotGroupCells(map)).GetOrDefault(EmptyList);
        }

        public virtual Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return Parent.GetCellPatternColor(cellPattern);
        }
    }

    public class ProductOutputCellResolver : OutputCellResolver
    {
        public override Option<IntVec3> OutputCell(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot)
        {
            return center.GetThingList(map)
                .Where(t => t.def == def)
                .OfType<IProductOutput>()
                .FirstOption()
                .Select(b => b.OutputCell());
        }
    }

    public interface ITargetCellResolver
    {
        ModExtension_WorkIORange Parent { get; set; }
        bool NeedClearingCache { get; }
        IEnumerable<IntVec3> GetRangeCells(ThingDef def, IntVec3 center, IntVec2 size, Map map, Rot4 rot, int range);
        Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern);
        int GetRange(float power);
    }

    public static class ITargetCellResolverExtension
    {
        public static int MaxRange(this ITargetCellResolver r)
        {
            return r.GetRange(r.Parent.maxPower);
        }

        public static int MinRange(this ITargetCellResolver r)
        {
            return r.GetRange(r.Parent.minPower);
        }
    }

    public abstract class BaseTargetCellResolver : ITargetCellResolver
    {
        public abstract bool NeedClearingCache { get; }
        public ModExtension_WorkIORange Parent { get; set; }

        public virtual int GetRange(float power)
        {
            return Mathf.RoundToInt(power / 500) + 3;
        }

        public virtual Color GetColor(IntVec3 cell, Map map, Rot4 rot, CellPattern cellPattern)
        {
            return Parent.GetCellPatternColor(cellPattern);
        }

        public abstract IEnumerable<IntVec3> GetRangeCells(ThingDef def, IntVec3 center, IntVec2 size, Map map,
            Rot4 rot, int range);
    }

    public enum CellPattern
    {
        BlurprintMin,
        BlurprintMax,
        Instance,
        OtherInstance,
        OutputCell,
        OutputZone,
        InputCell,
        InputZone
    }
}