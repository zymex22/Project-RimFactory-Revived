using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ProjectRimFactory
{
    [StaticConstructorOnStartup]
    public class PickUpAndHaulCompatiblity
    {
        static PickUpAndHaulCompatiblity()
        {
            if (!LoadedModManager.RunningModsListForReading.Any(mcp => mcp.PackageId == "Mehni.PickUpAndHaul")) return;
            var assemblyName = new AssemblyName("PRF_PUAH_Compatiblity_Assembly");
            // .NET (RimWorld 1.7+) has no RunAndSave and no AppDomain.DefineDynamicAssembly.
            var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(assemblyName.Name);
            // GenTypes.AllTypes skips assemblies whose types cannot all be loaded, unlike Assembly.GetTypes().
            var mehniIHoldMultipleThings = GenTypes.AllTypes
                .FirstOrDefault(t => t.FullName == "IHoldMultipleThings.IHoldMultipleThings");
            if (mehniIHoldMultipleThings == null)
            {
                Log.Error("PRF detected Pick Up And Haul loaded, but could not find IHoldMultipleThings\nHauling is likely to fail.");
                return;
            }

            foreach (var storageType in GenTypes.AllTypes
                         .Where(t => t.IsPublic || t.IsNestedPublic) // only publicly available types can be derived from
                         .Where(t => typeof(Storage.Building_MassStorageUnit).IsAssignableFrom(t))
                         .Where(t => !t.IsAbstract)
                         .ToList() // new types are defined inside the loop
                    )
            {
                // Build a magic derived class that also implements Mehni's interface
                var tb = mb.DefineType("PRF_PUAH_" + storageType.Name, TypeAttributes.Public,
                    storageType, new Type[] { mehniIHoldMultipleThings });
                // Make sure the new type has a default constructor (not sure this is needed, but I bet it is)
                tb.DefineDefaultConstructor(MethodAttributes.Public);
                var replacementType = tb.CreateType();
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.thingClass == storageType) def.thingClass = replacementType;
                }
            }
        }
    }
}