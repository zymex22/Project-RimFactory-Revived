using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory
{
    [StaticConstructorOnStartup]
    public class PickUpAndHaulCompatiblity
    {
        static PickUpAndHaulCompatiblity()
        {
            if (LoadedModManager.RunningModsListForReading.Any(mcp => mcp.PackageId == "Mehni.PickUpAndHaul"))
            {
                var assemblyName = new AssemblyName("PRF_PUAH_Compatiblity_Assembly");
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                ModuleBuilder mb = ab.DefineDynamicModule(assemblyName.Name);
                Type mehniIHoldMultipleThings = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
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
                    TypeBuilder tb = mb.DefineType("PRF_PUAH_" + storageType.Name, TypeAttributes.Public,
                        storageType, new Type[] { mehniIHoldMultipleThings });
                    // Make sure the new type has a default constructor (not sure this is needed, but I bet it is)
                    tb.DefineDefaultConstructor(MethodAttributes.Public);
                    Type replacementType = tb.CreateType();
                    foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (def.thingClass == storageType) def.thingClass = replacementType;
                    }
                }
            }
        }
    }
}