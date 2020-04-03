using RimWorld;
using Verse;
using System;
using System.Reflection;

namespace ProjectRimFactory.Common
{
    [StaticConstructorOnStartup]
    public static class PRFDefOf
    {
        static PRFDefOf()
        {
            Type thisType = typeof(PRFDefOf);
            FieldInfo[] fields = thisType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                object def = GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), fields[i].FieldType, "GetNamedSilentFail", fields[i].Name);
                if (def != null)
                {
                    fields[i].SetValue(null, def);
                }
            }
        }
        public static JobDef PRFDrone_ReturnToStation;
        public static JobDef PRFDrone_SelfTerminate;

        public static PawnKindDef PRFDroneKind;
        public static PawnKindDef PRFSlavePawn;

        public static ResearchProjectDef PRFAtomicReconstruction;
        public static ResearchProjectDef PRFNanoMaterials;
        public static ResearchProjectDef PRFEdiblesSynthesis;
        public static ResearchProjectDef PRFManufacturablesProduction;
        public static ResearchProjectDef PaperclipGeneratorSelfImprovement;
        public static ResearchProjectDef PaperclipGeneratorKugelblitz;
        public static ResearchProjectDef PaperclipGeneratorQuantumFoamManipulation;
        public static ResearchProjectDef PRFOrdoDataRummaging;
        public static ResearchProjectDef PRFVanometrics;

        public static TerrainDef PRFFloorComputer;
        public static TerrainDef PRFZCompositeTile;
        public static TerrainDef PRFYCompositeTile;

        public static ThingDef Paperclip;
        public static ThingDef PRFXComposite;
        public static ThingDef PRFYComposite;
        public static ThingDef PRFZComposite;
        public static ThingDef PRFVolatiteChunk;
        public static ThingDef PRFDrone;
        public static ThingDef PRFDroneModule;

        public static FleshTypeDef PRFDroneFlesh;
    }
}
