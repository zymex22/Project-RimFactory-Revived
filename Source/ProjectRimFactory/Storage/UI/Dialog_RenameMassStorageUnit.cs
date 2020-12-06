using RimWorld;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_RenameMassStorageUnit : Dialog_Rename
    {
        private readonly Building_MassStorageUnit building;

        public Dialog_RenameMassStorageUnit(Building_MassStorageUnit building)
        {
            this.building = building;
            curName = building.uniqueName ?? building.LabelNoCount;
        }

        protected override void SetName(string name)
        {
            building.uniqueName = curName;
            Messages.Message("PRFStorageBuildingGainsName".Translate(curName), MessageTypeDefOf.TaskCompletion);
        }
    }
}