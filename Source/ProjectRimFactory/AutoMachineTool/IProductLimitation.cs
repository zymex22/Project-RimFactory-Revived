using RimWorld;

namespace ProjectRimFactory.AutoMachineTool
{
    // used in settings ITab and AutoMachineTool buildings that 
    //   do something until a limit is reached.
    internal interface IProductLimitation
    {
        int ProductLimitCount { get; set; }
        bool ProductLimitation { get; set; }
        bool CountStacks { get; set; }
        Option<SlotGroup> TargetSlotGroup { get; set; }
    }
}