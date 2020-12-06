using RimWorld;

namespace ProjectRimFactory.Common
{
    // Have an ITab_Storage that says "Filter" instead of "Storage"
    internal class ITab_Filter : ITab_Storage
    {
        public ITab_Filter()
        {
            labelKey = "Filter";
        }
        // Everything else is vanilla, so any changes anyone makes to ITab_Storage
        //   (such as RSA's search function!) *should* work just fine for us!
#if false
        private static readonly Vector2 WinSize = new Vector2(300f, 500f);

        public ITab_PullerFilter()
        {
            this.size = WinSize;
            this.labelKey = "PRF.AutoMachineTool.Puller.OutputItemFilter.TabName";

            this.description = "PRF.AutoMachineTool.Puller.OutputItemFilter.Description".Translate();
        }

        private string description;

        private Building_ItemPuller Puller
        {
            get => (Building_ItemPuller)this.SelThing;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            this.groups = this.Puller.Map.haulDestinationManager.AllGroups.ToList();
        }

        private List<SlotGroup> groups;

        public override bool IsVisible => Puller.Filter != null;

        private Vector2 scrollPosition;

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap();

            var rect = list.GetRect(40f);
            Widgets.Label(rect, this.description);
            list.Gap();

            rect = list.GetRect(30f);
            if (Widgets.ButtonText(rect, "PRF.AutoMachineTool.Puller.FilterCopyFrom".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(groups.Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(), () => this.Puller.Filter.CopyAllowancesFrom(g.Settings.filter))).ToList()));
            }
            list.Gap();

            list.End();
            var height = list.CurHeight;

            ThingFilterUI.DoThingFilterConfigWindow(inRect.BottomPartPixels(inRect.height - height), ref this.scrollPosition, this.Puller.Filter);

        }
#endif
    }
}