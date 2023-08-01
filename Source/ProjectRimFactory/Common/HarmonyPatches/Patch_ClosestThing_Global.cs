using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    [HarmonyPatch]
    class Patch_ClosestThing_Global
    {
        /*
        Need to path the internal "	void Process(Thing t)" Method to skip the Spawned Check for Items contained in the ASU
        That can only be done a Transpiler

        The adding of the Items contained in the ASU can't be done at this point as we might not have access to the current map.
        (One could get the map from the spawned Things, but there is the chance that there are no spawned things...)
        */

        //TODO 
        //Might also need to patch g__Process|6_0
        //Same code should work
        public static Type predicateClass;
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {
            predicateClass = typeof(GenClosest);

            var m = predicateClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("g__Process|5_0"));
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_ClosestThing_Global.TargetMethod()");
            }
            return m;
        }



        static bool Patched = false;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            
            foreach (var instruction in instructions)
            {
                if(instruction.opcode == OpCodes.Callvirt && !Patched)
                {
                    Patched = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_ClosestThing_Global),
                        nameof(Patch_ClosestThing_Global.SpawnedCheck), new[] { typeof(Thing) }));
                    continue;
                }

                yield return instruction;

            }

        }

        static bool SpawnedCheck(Thing thing)
        {
            //TODO Might want to add a check if there is a connecting port
            if (thing.Spawned)
            {
                return true;
            }
            else
            {
                return thing.ParentHolder is Building_ColdStorage;
            }
        }

    }
    [HarmonyPatch]
    class Patch_ClosestThing_Global2
    {
        /*
        Need to path the internal "	void Process(Thing t)" Method to skip the Spawned Check for Items contained in the ASU
        That can only be done a Transpiler

        The adding of the Items contained in the ASU can't be done at this point as we might not have access to the current map.
        (One could get the map from the spawned Things, but there is the chance that there are no spawned things...)
        */

        public static Type predicateClass;
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {
            predicateClass = typeof(GenClosest);

            var m = predicateClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("g__Process|6_0"));
            if (m == null)
            {
                Log.Error("PRF Harmony Error - m == null for Patch_ClosestThing_Global.TargetMethod()");
            }
            return m;
        }



        static bool Patched = false;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && !Patched)
                {
                    Patched = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_ClosestThing_Global2),
                        nameof(Patch_ClosestThing_Global2.SpawnedCheck), new[] { typeof(Thing) }));
                    continue;
                }

                yield return instruction;

            }

        }

        static bool SpawnedCheck(Thing thing)
        {
            //TODO Might want to add a check if there is a connecting port
            if (thing.Spawned)
            {
                return true;
            }
            else
            {
                return thing.ParentHolder is Building_ColdStorage;
            }
        }

    }


    [HarmonyPatch(typeof(GenClosest), "ClosestThingReachable")]
    class Patch_ClosestThingReachable
    {
        public static void Postfix(Thing __result, ThingRequest thingReq)
        {
         //   Log.Message($"ClosestThingReachable returns {__result} for {thingReq} --  region: {thingReq.CanBeFoundInRegion}");
        }


        static bool Patched = false;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
               // Log.Message($"{instruction.opcode} - {instruction.operand}");
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "System.Collections.Generic.IEnumerable`1[Verse.Thing] (7)" && !Patched)
                {
                    Patched = true;
                    //Load the Map
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_ClosestThingReachable),
                        nameof(Patch_ClosestThingReachable.ThingsWithColdStorage), new[] { typeof(IEnumerable<Thing>), typeof(Map)  }));


                }

                yield return instruction;

            }

        }

        public static IEnumerable<Thing> ThingsWithColdStorage(IEnumerable<Thing> baseList, Map map)
        {
            List<Thing> things = baseList.ToList();

            var mapComp = PatchStorageUtil.GetPRFMapComponent(map);
            var storage = mapComp.ColdStorageBuildings;
            foreach(var b in storage)
            {
                things.AddRange((b as Building_ColdStorage).StoredItems);
              //  Log.Message($"added  items form {b}");
            }

            return things;

        }


    }


    //Works but not enough
    [HarmonyPatch(typeof(ItemAvailability), "ThingsAvailableAnywhere")]
    class Patch_ItemAvailability_ThingsAvailableAnywhere
    {

        public static void Postfix(ref bool __result, ThingDefCountClass need, Pawn pawn, Map ___map)
        {
            if (__result) return;

            var ColdStorages = PatchStorageUtil.GetPRFMapComponent(___map).ColdStorageBuildings;
            foreach(Building_ColdStorage b in ColdStorages)
            {
                int cnt = need.count;

                var items = b.StoredItems;
                for(int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    
                    if (item.def == need.thingDef)
                    {
                        
                        cnt -= item.stackCount;
                    }
                    if(cnt <= 0)
                    {
                        
                        __result = true;
                        return;
                    }

                }
            }
        }


    }


    //Need to Patch RegionwiseBFSWorker

    //Verse.RegionProcessorClosestThingReachable
    //override bool RegionProcessor(Region reg)
    //Need a Transpiler
    /*
     List<Thing> list = reg.ListerThings.ThingsMatching(req);
    Add Items Accesible via IO Advanced Ports in that Area
    Be aware of duplicates

    Be aware of distance calc




    "//Note that could also enhance the Logic for DSU Access and vs stuff on the floor"
     
     */
  /*  [HarmonyPatch(typeof(RegionProcessorClosestThingReachable), "RegionProcessor")]
    public class Patch_RegionProcessorClosestThingReachable_RegionProcessor
    {

        static bool patchedAppend = false;


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool lastWas_ldloc2 = false;
            bool foundThingPos = false;
            bool foundThingPosDone = false;
            int callCnt = 0;
            //req Thing request
            //reg region
            foreach (var instruction in instructions)
            {

                if(!foundThingPosDone && lastWas_ldloc2 && instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString() == "Verse.IntVec3 get_Position()")
                {
                    //Log.Message();
                    foundThingPos = true;
                }
                if(foundThingPos && instruction.opcode != OpCodes.Call)
                {
                    continue;
                }else if(foundThingPos)
                {
                    callCnt++;
                    if(callCnt == 2)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RegionProcessorClosestThingReachable), "root"));
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                            typeof(Patch_RegionProcessorClosestThingReachable_RegionProcessor),
                            nameof(Patch_RegionProcessorClosestThingReachable_RegionProcessor.CalcDistance), new[] { typeof(Thing), typeof(IntVec3), typeof(Verse.Region) }));
                        foundThingPosDone = true;
                        foundThingPos = false;
                    }
                    
                    continue;
                }



                lastWas_ldloc2 = instruction.opcode == OpCodes.Ldloc_2;


                if (instruction.opcode == OpCodes.Stloc_0 && !patchedAppend)
                {
                    patchedAppend = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RegionProcessorClosestThingReachable),"req"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(Patch_RegionProcessorClosestThingReachable_RegionProcessor),
                        nameof(Patch_RegionProcessorClosestThingReachable_RegionProcessor.AppendIOStuff), new[] { typeof(List<Thing>), typeof(Verse.Region), typeof(ThingRequest) }));
                }
            
                yield return instruction;

            }

        }

        //maybe use by ref?!

        static List<Thing> AppendIOStuff(List<Thing> things, Verse.Region region, ThingRequest req)
        {
            if(things is null)
            {
                Log.Warning("things List is Null AppendIOStuff");
                things = new List<Thing>(); 
            }
            if (region is null)
            {
                Log.Warning("region is null");
                return things;
            }
            


            var AdvancePorts = region.ListerThings.ThingsOfDef(PRFDefOf.PRF_IOPort_II);
            if(AdvancePorts is null)
            {
                Log.Warning("AdvancePorts is null");
                return things;
            }
            foreach(var thing in AdvancePorts)
            {
                Storage.Building_AdvancedStorageUnitIOPort port =  thing as Storage.Building_AdvancedStorageUnitIOPort;
                var Items = port?.boundStorageUnit?.StoredItems;
                if (Items == null) continue;
                for(int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (req.Accepts(item) && !things.Contains(item))
                    {
                        things.Add(item);
                    }
                }
            }
            return things;
        }

        //temp
        public static IntVec3 ClosesetPort = new IntVec3(167,0,149);
        static float CalcDistance(Thing thing, IntVec3 root, Verse.Region region)
        {

            if (thing.ParentHolder is Building_ColdStorage)
            {
                //Need to fined the closet linked Advance IO Port
                //And calc the Position using that
                var mapcomp = PatchStorageUtil.GetPRFMapComponent(region.Map);
                var Ports = mapcomp.GetadvancedIOLocations;
                var port_cnt = Ports.Count();
                float Mindist = float.MaxValue;
                for (int i = 0; i< port_cnt; i++)
                {
                    var Port = Ports.ElementAt(i);
                    if(Port.Value.boundStorageUnit == thing.ParentHolder) {
                        var dist = (Port.Key - root).LengthHorizontalSquared;
                        if(dist < Mindist)
                        {
                            Mindist = dist;
                            ClosesetPort = Port.Key;
                        //    Log.Message($"Distance for {Port.Value} is {dist} - getting {thing}");
                        }


                    }
                }
                return Mindist;


            }
            else
            {
                return (thing.Position - root).LengthHorizontalSquared;
            }
        }

    
    
    }
    

    [HarmonyPatch(typeof(ReachabilityWithinRegion), "ThingFromRegionListerReachable")]
    public class Patch_ReachabilityWithinRegion_ThingFromRegionListerReachable
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool done = false;  
            bool foundThing = false;

            foreach (var instruction in instructions)
            {
                if(foundThing && !done)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                       typeof(Patch_ReachabilityWithinRegion_ThingFromRegionListerReachable),
                       nameof(Patch_ReachabilityWithinRegion_ThingFromRegionListerReachable.GetPosition), new[] { typeof(Thing)}));
                    done = true;
                    continue;
                }
                if(!done && instruction.opcode == OpCodes.Ldarg_0)
                {
                    foundThing = true;
                }
                


                yield return instruction;

            }

        }


        static IntVec3 GetPosition(Thing thing)
        {
            if (thing.ParentHolder is Building_ColdStorage building_)
            {
                var pos = Patch_RegionProcessorClosestThingReachable_RegionProcessor.ClosesetPort;
                if (!pos.IsValid)
                {
                    Log.Error("PRF GetPosition Post Pos is Invalid");
                    pos = building_.Position;
                }

             //   Log.Message($"PRF GetPosition {pos}");
                return pos;
            }
            else
            {
                return thing.Position;
            }
        } 
    }



    /*
     
     Katia started 10 jobs in one tick. newJob=HaulToContainer (Job_61312) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429 jobGiver=RimWorld.JobGiver_Work jobList=
 (Wait_Combat (Job_60887) A=(169, 0, 159))
 (BuildRoof (Job_61106) A=(172, 0, 156) B=(172, 0, 156)) 
 (HaulToContainer (Job_61296) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61298) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61300) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61302) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61304) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61306) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61308) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61310) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429) 
 (HaulToContainer (Job_61312) A=Thing_Steel66075 B=Thing_Blueprint_Wall68430 C=Thing_Blueprint_Wall68429)  

    Maybe a job interruption is the issue
     
     */


}
