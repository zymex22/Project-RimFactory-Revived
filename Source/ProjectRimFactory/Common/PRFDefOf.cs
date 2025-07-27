using RimWorld;
using System;
using System.Reflection;
using Verse;
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

namespace ProjectRimFactory.Common
{
    [StaticConstructorOnStartup]
    public static class PRFDefOf
    {
        static PRFDefOf()
        {
            var thisType = typeof(PRFDefOf);
            var fields = thisType.GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                var def = GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), fields[i].FieldType, "GetNamedSilentFail", fields[i].Name);
                if (def != null)
                {
                    fields[i].SetValue(null, def);
                }
            }
        }
        public static JobDef PRFStaticJob;

        public static JobDef PRFDrone_ReturnToStation;
        public static JobDef PRFDrone_SelfTerminate;

        public static PawnKindDef PRFDroneKind;
        public static PawnKindDef PRFSlavePawn;

        public static ResearchProjectDef PRFAtomicReconstruction;
        public static ResearchProjectDef PRFEdiblesSynthesis;
        public static ResearchProjectDef PRFManufacturablesProduction;
        public static ResearchProjectDef PaperclipGeneratorSelfImprovement;
        public static ResearchProjectDef PaperclipGeneratorKugelblitz;
        public static ResearchProjectDef PaperclipGeneratorQuantumFoamManipulation;
        public static ResearchProjectDef PRFOrdoDataRummaging;

        public static TerrainDef PRFFloorComputer;
        public static TerrainDef PRFZCompositeTile;
        public static TerrainDef PRFYCompositeTile;

        public static ThingDef Paperclip;
        public static ThingDef PRFDrone;
        public static ThingDef PRF_DroneModule;
        public static ThingDef Column;
        public static ThingDef PRF_MiniDroneColumn;
        public static ThingDef PRF_Recycler;

        public static BackstoryDef ChildSpy47;
        public static BackstoryDef ColonySettler53;


        //Research Projects
        public static ResearchProjectDef PRF_BasicDrones;
        public static ResearchProjectDef PRF_ImprovedDrones; //For Level 15
        public static ResearchProjectDef PRF_AdvancedDrones; //For Level 20


        public static TraderKindDef PRF_Factory_Supplier;

    }
}
