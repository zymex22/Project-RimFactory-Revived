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
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var mb = ab.DefineDynamicModule(assemblyName.Name);
            var mehniIHoldMultipleThings = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "IHoldMultipleThings.IHoldMultipleThings");
            if (mehniIHoldMultipleThings == null)
            {
                Log.Error("PRF detected Pick Up And Haul loaded, but could not find IHoldMultipleThings\nHauling is likely to fail.");
                return;
            }

            foreach (var storageType in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(a => a.GetExportedTypes()) // GetTypes() more general, Exported is only publicly available types
                         .Where(t => typeof(Storage.Building_MassStorageUnit).IsAssignableFrom(t))
                         .Where(t => !t.IsAbstract)
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