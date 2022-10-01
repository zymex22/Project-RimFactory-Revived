using System.Xml;
using Verse;

namespace ProjectRimFactory.Common
{
    public class PatchOperationRTFuseInsertionHook : PatchOperationAdd
    {
        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (GenTypes.GetTypeInAnyAssembly("RT_Fuse.CompRTFuse") != null)
            {
                return base.ApplyWorker(xml);
            }
            return true;
        }
    }
}
