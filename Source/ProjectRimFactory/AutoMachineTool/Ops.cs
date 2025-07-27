using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public static class Ops
    {
        public static Option<T> Option<T>(T value)
        {
            return new Option<T>(value);
        }

        public static Nothing<T> Nothing<T>()
        {
            return new Nothing<T>();
        }

        private static Just<T> Just<T>(T value)
        {
            return new Just<T>(value);
        }

        public static Option<T> FirstOption<T>(this IEnumerable<T> e)
        {
            var en = e.GetEnumerator();
            if (en.MoveNext())
            {
                return new Just<T>(en.Current);
            }
            return new Nothing<T>();
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> selector)
        {
            return source.SelectMany(e => selector(e).ToList());
        }

        public static List<T> Append<T>(this List<T> lhs, T rhs)
        {
            lhs.Add(rhs);
            return lhs;
        }

        public static List<T> Head<T>(this List<T> lhs, T rhs)
        {
            lhs.Insert(0, rhs);
            return lhs;
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence) action(item);
        }

        public static Option<V> GetOption<K, V>(this Dictionary<K, V> dict, K key)
        {
            V val;
            if (dict.TryGetValue(key, out val))
            {
                return Just(val);
            }
            return Nothing<V>();
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        public static List<List<T>> Grouped<T>(this List<T> source, int size)
        {
            var l = new List<List<T>>();
            var idx = 0;
            while (idx < source.Count)
            {
                var count = Mathf.Min(size, source.Count - idx);
                l.Add(source.GetRange(idx, count));
                idx += count;
            }
            return l;
        }

        public static IEnumerable<T> GetEnumValues<T>() where T : struct
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        #region for rimworld
#if DEBUG
        public static void L(object obj) { Log.Message(obj == null ? "null" : obj.ToString()); }
#endif

        public static TO CopyTo<FROM, TO>(this FROM bill, TO copy) where FROM : Bill_Production where TO : Bill_Production
        {
            //Todo: Check if we are not missing things
            copy.allowedSkillRange = bill.allowedSkillRange;
            copy.billStack = bill.billStack;
            copy.deleted = bill.deleted;
            copy.hpRange = bill.hpRange;
            copy.includeEquipped = bill.includeEquipped;
            copy.SetIncludeGroup(bill.GetIncludeSlotGroup());
            copy.includeTainted = bill.includeTainted;
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.limitToAllowedStuff = bill.limitToAllowedStuff;
            copy.paused = bill.paused;
            copy.pauseWhenSatisfied = bill.pauseWhenSatisfied;
            copy.SetPawnRestriction(bill.PawnRestriction);
            copy.qualityRange = bill.qualityRange;
            copy.recipe = bill.recipe;
            copy.repeatCount = bill.repeatCount;
            copy.repeatMode = bill.repeatMode;
            copy.SetStoreMode(bill.GetStoreMode());
            copy.suspended = bill.suspended;
            copy.targetCount = bill.targetCount;
            copy.unpauseWhenYouHave = bill.unpauseWhenYouHave;
            copy.precept = bill.precept;

            return copy;
        }

        public static IntVec3 FacingCell(IntVec3 center, IntVec2 size, Rot4 rot)
        {
            var list = GenAdj.CellsOccupiedBy(center, rot, size).ToList();
            var minX = list.Min(c => c.x);
            var maxX = list.Max(c => c.x);
            var minZ = list.Min(c => c.z);
            var maxZ = list.Max(c => c.z);
            var x = rot.FacingCell.x == 0 ? center.x : (rot.FacingCell.x > 0 ? maxX + 1 : minX - 1);
            var z = rot.FacingCell.z == 0 ? center.z : (rot.FacingCell.z > 0 ? maxZ + 1 : minZ - 1);
            return new IntVec3(x, center.y, z);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 pos, Rot4 dir, int range)
        {
            return FacingRect(pos, new IntVec2(1, 1), dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(Thing thing, Rot4 dir, int range)
        {
            return FacingRect(thing.Position, thing.RotatedSize, dir, range);
        }

        public static IEnumerable<IntVec3> FacingRect(IntVec3 center, IntVec2 size, Rot4 dir, int range)
        {
            Util.CounterAdjustForRotation(ref center, ref size, dir);
            var facing = FacingCell(center, size, dir);
            var xoffset = dir.FacingCell.x * range + facing.x;
            var zoffset = dir.FacingCell.z * range + facing.z;
            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    yield return new IntVec3(x + xoffset, center.y, z + zoffset);
                }
            }
        }

        public static Rot4 RotateAsNew(this Rot4 rot, RotationDirection dir)
        {
            var n = rot;
            n.Rotate(dir);
            return n;
        }

        public static bool IsAdult(this Pawn p)
        {
            return p.ageTracker.CurLifeStageIndex >= 2;
        }

        public static Color A(this Color color, float a)
        {
            var c = color;
            c.a = a;
            return c;
        }

        #endregion
    }

    public class Option<T>
    {
        private readonly T value;

        public Option(T val)
        {
            if (val is null) return;
            value = val;
            HasValue = true;
        }

        public T Value => value;

        public bool HasValue { get; } = false;

        public Option()
        {
        }

        public Option<TO> Select<TO>(Func<T, TO> func)
        {
            return HasValue ? new Option<TO>(func(value)) : new Nothing<TO>();
        }

        public void ForEach(Action<T> act)
        {
            if (HasValue) act(value);
        }

        public List<T> ToList()
        {
            return HasValue ? new List<T>(new T[] { value }) : new List<T>();
        }

        public T GetOrDefault(T defaultValue)
        {
            return HasValue ? value : defaultValue;
        }

        public Func<Func<T, R>, R> Fold<R>(R defaultValue)
        {
            return HasValue ? (f) => f(value) : (Func<Func<T, R>, R>)((_) => defaultValue);
        }

        public Func<Func<T, R>, R> Fold<R>(Func<R> craetor)
        {
            return HasValue ? (f) => f(value) : (Func<Func<T, R>, R>)((_) => craetor());
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Option<T> && HasValue == ((Option<T>)obj).HasValue &&
                (!HasValue || value.Equals(((Option<T>)obj).value));
        }

        public override int GetHashCode()
        {
            return HasValue ? value.GetHashCode() : HasValue.GetHashCode();
        }

        public override string ToString()
        {
            return Fold("Option<Nothing>")(v => "Option<" + v.ToString() + ">");
        }
    }

    public class Nothing<T> : Option<T>
    {
        public Nothing() : base()
        {
        }
    }

    public class Just<T> : Option<T>
    {
        public Just(T value) : base(value)
        {
        }
    }
}
