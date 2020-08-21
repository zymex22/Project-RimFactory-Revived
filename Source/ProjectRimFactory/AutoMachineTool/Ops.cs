using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

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

        public static Just<T> Just<T>(T value)
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
            else
            {
                return new Nothing<T>();
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> selector)
        {
            return source.SelectMany(e => selector(e).ToList());
        }

        public static List<T> Append<T>(this List<T> lhs, List<T> rhs)
        {
            lhs.AddRange(rhs);
            return lhs;
        }

        public static List<T> Append<T>(this List<T> lhs, T rhs)
        {
            lhs.Add(rhs);
            return lhs;
        }

        public static List<T> Ins<T>(this List<T> lhs, int index, T rhs)
        {
            lhs.Insert(index, rhs);
            return lhs;
        }

        public static List<T> Head<T>(this List<T> lhs, T rhs)
        {
            lhs.Insert(0, rhs);
            return lhs;
        }

        public static List<T> Del<T>(this List<T> lhs, T rhs)
        {
            lhs.Remove(rhs);
            return lhs;
        }

        public static Option<T> ElementAtOption<T>(this List<T> list, int index)
        {
            if (index >= list.Count)
            {
                return new Nothing<T>();
            }
            return Option(list[index]);
        }

        public static bool EqualValues<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            var l = lhs.ToList();
            var r = rhs.ToList();
            if (l.Count == r.Count)
            {
                for (int i = 0; i < l.Count; i++)
                {
                    if (!l[i].Equals(r[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence) action(item);
        }

        public static IEnumerable<T> Peek<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence)
            {
                action(item);
                yield return item;
            }
        }

        public static Option<T> FindOption<T>(this List<T> sequence, Predicate<T> predicate)
        {
            var i = sequence.FindIndex(predicate);
            if (i == -1)
            {
                return new Nothing<T>();
            }
            else
            {
                return new Just<T>(sequence[i]);
            }
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

        public static Tuple<T1, T2> Tuple<T1, T2>(T1 v1, T2 v2)
        {
            return new Tuple<T1, T2>(v1, v2);
        }

        public static Tuple<T1, T2, T3> Tuple<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
        {
            return new Tuple<T1, T2, T3>(v1, v2, v3);
        }

        public static IEnumerable<IEnumerable<T>> Grouped<T>(this IEnumerable<T> source, int size)
        {
            while (source.Any())
            {
                yield return source.Take(size);
                source = source.Skip(size);
            }
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

        public static bool PlaceItemXXX(Thing t, IntVec3 cell, bool forbid, Map map, bool firstAbsorbStack = false)
        {
            Action<Thing> effect = (item) =>
            {
                item.def.soundDrop.PlayOneShot(item);
                MoteMaker.ThrowDustPuff(item.Position, map, 0.5f);
            };

            Func<bool> absorb = () =>
            {
                cell.SlotGroupCells(map).SelectMany(c => c.GetThingList(map)).Where(i => i.def == t.def).ForEach(i => i.TryAbsorbStack(t, true));
                if (t.stackCount == 0)
                {
                    effect(t);
                    return true;
                }
                return false;
            };
            // fast check:
            if (!firstAbsorbStack && cell.GetThingList(map).Where(ti => ti.def.category == ThingCategory.Item).Count() == 0)
            {
                if (t.Spawned) t.DeSpawn();
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            if (absorb())
                return true;
            // IsValidStorageFor should also work for multi-storage mods
            if (StoreUtility.IsValidStorageFor(cell, map, t)) {
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Direct);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            var o = cell.SlotGroupCells(map).Where(c => c.IsValidStorageFor(map, t))
                .Where(c => c.GetThingList(map).Where(b => b.def.category == ThingCategory.Building).All(b => !(b is Building_BeltConveyor)))
                .FirstOption();
            if (o.HasValue)
            {
                if (t.Spawned) t.DeSpawn();
                GenPlace.TryPlaceThing(t, o.Value, map, ThingPlaceMode.Near);
                if (forbid) t.SetForbidden(forbid);
                effect(t);
                return true;
            }
            return false;
        }

        public static void Noop()
        {
        }

        public static List<IntVec3> SlotGroupCells(this IntVec3 c, Map map)
        {
            return Option(map.haulDestinationManager.SlotGroupAt(c)).Select(g => g.CellsList).GetOrDefault(new List<IntVec3>().Append(c));
        }

        public static TO CopyTo<FROM, TO>(this FROM bill, TO copy) where FROM : Bill_Production where TO : Bill_Production
        {
            copy.allowedSkillRange = bill.allowedSkillRange;
            copy.billStack = bill.billStack;
            copy.deleted = bill.deleted;
            copy.hpRange = bill.hpRange;
            copy.includeEquipped = bill.includeEquipped;
            copy.includeFromZone = bill.includeFromZone;
            copy.includeTainted = bill.includeTainted;
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.lastIngredientSearchFailTicks = bill.lastIngredientSearchFailTicks;
            copy.limitToAllowedStuff = bill.limitToAllowedStuff;
            copy.paused = bill.paused;
            copy.pauseWhenSatisfied = bill.pauseWhenSatisfied;
            copy.pawnRestriction = bill.pawnRestriction;
            copy.qualityRange = bill.qualityRange;
            copy.recipe = bill.recipe;
            copy.repeatCount = bill.repeatCount;
            copy.repeatMode = bill.repeatMode;
            copy.SetStoreMode(bill.GetStoreMode());
            copy.suspended = bill.suspended;
            copy.targetCount = bill.targetCount;
            copy.unpauseWhenYouHave = bill.unpauseWhenYouHave;

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

        public static Option<IPlantToGrowSettable> GetPlantable(this IntVec3 pos, Map map)
        {
            /*
            return Option(pos.GetZone(map) as IPlantToGrowSettable)
                .Fold(() => pos.GetThingList(map).Where(t => t.def.category == ThingCategory.Building).SelectMany(t => Option(t as IPlantToGrowSettable)).FirstOption())
                (x => Option(x));
            */
            var p = pos.GetZone(map) as IPlantToGrowSettable;
            if (p != null)
            {
                return Option(p);
            }
            foreach (var t in pos.GetThingList(map))
            {
                if (t.def.category == ThingCategory.Building)
                {
                    p = t as IPlantToGrowSettable;
                    if (p != null)
                        return Option(p);
                }
            }
            return Nothing<IPlantToGrowSettable>();
        }

        public static bool IsAdult(this Pawn p)
        {
            return p.ageTracker.CurLifeStageIndex >= 2;
        }

        public static Color A(this Color color, float a)
        {
            Color c = color;
            c.a = a;
            return c;
        }

        public static float GetEnergyAmount(ThingDef def)
        {
            return ConvertEnergyAmount(StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, null)));
        }

        public static float GetEnergyAmount(ThingDef def, ThingDef stuffDef)
        {
            return ConvertEnergyAmount(StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, stuffDef)));
        }

        public static float ConvertEnergyAmount(float marketValue)
        {
            return marketValue * 0.1f;
        }
        #endregion

        public static Func<T, TValue> GenerateGetFieldDelegate<T, TValue>(FieldInfo field)
        {
            var d = new DynamicMethod("getter", typeof(TValue), new Type[] { typeof(T) }, true);
            var g = d.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldfld, field);
            g.Emit(OpCodes.Ret);

            return (Func<T, TValue>)d.CreateDelegate(typeof(Func<T, TValue>));
        }

        public static Action<T, TValue> GenerateSetFieldDelegate<T, TValue>(FieldInfo field)
        {
            var d = new DynamicMethod("setter", typeof(void), new Type[] { typeof(T), typeof(TValue) }, true);
            var g = d.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Stfld, field);
            g.Emit(OpCodes.Ret);

            return (Action<T, TValue>)d.CreateDelegate(typeof(Action<T, TValue>));
        }

        public static Func<T, TResult> GenerateMeshodDelegate<T, TResult>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Func<T, TResult>>(callExp, new List<ParameterExpression>().Append(instanceParam).Append(args)).Compile();
        }

        public static Func<T, TParam1, TResult> GenerateMeshodDelegate<T, TParam1, TResult>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { Expression.Parameter(typeof(TParam1), "param1") };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Func<T, TParam1, TResult>>(callExp, new List<ParameterExpression>().Append(instanceParam).Append(args)).Compile();
        }

        public static Action<T> GenerateVoidMeshodDelegate<T>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Action<T>>(callExp, new List<ParameterExpression>().Append(instanceParam)).Compile();
        }

        public static Action<T, TParam1> GenerateVoidMeshodDelegate<T, TParam1>(MethodInfo getter)
        {
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var args = new List<ParameterExpression>() { Expression.Parameter(typeof(TParam1), "param1") };
            var callExp = Expression.Call(instanceParam, getter, args.Cast<Expression>());
            return Expression.Lambda<Action<T, TParam1>>(callExp, new List<ParameterExpression>().Append(instanceParam)).Compile();
        }
    }

    public class Option<T>
    {
        private readonly T value;

        private bool hasValue = false;

        public Option(T val)
        {
            if (val != null)
            {
                this.value = val;
                hasValue = true;
            }
        }

        public T Value
        {
            get { return value; }
        }

        public bool HasValue
        {
            get { return hasValue; }
        }

        public Option()
        {
        }

        public Option<TO> SelectMany<TO>(Func<T, Option<TO>> func)
        {
            return hasValue ? func(value) : new Nothing<TO>();
        }

        public Option<TO> Select<TO>(Func<T, TO> func)
        {
            return hasValue ? new Option<TO>(func(value)) : new Nothing<TO>();
        }

        public Option<T> Where(Predicate<T> pre)
        {
            return hasValue ? pre(this.value) ? this : new Nothing<T>() : new Nothing<T>();
        }

        public void ForEach(Action<T> act)
        {
            if (hasValue) act(this.value);
        }

        public List<T> ToList()
        {
            return this.hasValue ? new List<T>(new T[] { this.value }) : new List<T>();
        }

        public T GetOrDefault(T defaultValue)
        {
            return this.hasValue ? this.value : defaultValue;
        }

        public T GetOrDefaultF(Func<T> creator)
        {
            return this.hasValue ? this.value : creator();
        }

        public Func<Func<T, R>, R> Fold<R>(R defaultValue)
        {
            return this.hasValue ? (f) => f(this.value) : (Func<Func<T, R>, R>)((_) => defaultValue);
        }

        public Func<Func<T, R>, R> Fold<R>(Func<R> craetor)
        {
            return this.hasValue ? (f) => f(this.value) : (Func<Func<T, R>, R>)((_) => craetor());
        }

        public Option<T> Peek(Action<T> act)
        {
            if (hasValue)
            {
                act(this.value);
            }
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Option<T> && this.hasValue == ((Option<T>)obj).hasValue &&
                (!this.hasValue || this.value.Equals(((Option<T>)obj).value));
        }

        public override int GetHashCode()
        {
            return this.hasValue ? this.value.GetHashCode() : this.hasValue.GetHashCode();
        }

        public override string ToString()
        {
            return this.Fold("Option<Nothing>")(v => "Option<" + v.ToString() + ">");
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
