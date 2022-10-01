using RimWorld;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_RenameMassStorageUnit : Dialog_Rename
    {
        public Dialog_RenameMassStorageUnit(IRenameBuilding building)
        {
            this.building = building;
            curName = building.UniqueName ?? building.Building.LabelNoCount;
        }
        protected override void SetName(string name)
        {
            building.UniqueName = curName;
            Messages.Message("PRFStorageBuildingGainsName".Translate(curName), MessageTypeDefOf.TaskCompletion);
        }
        IRenameBuilding building;
    }
}
