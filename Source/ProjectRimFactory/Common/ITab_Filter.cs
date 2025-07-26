using RimWorld;

namespace ProjectRimFactory.Common
{
    // Have an ITab_Storage that says "Filter" instead of "Storage"
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    class ITab_Filter : ITab_Storage
    {
        public ITab_Filter()
        {
            labelKey = "Filter";
        }
        // Everything else is vanilla, so any changes anyone makes to ITab_Storage
        //   (such as RSA's search function!) *should* work just fine for us!
    }
}
