using System.Xml;
using Verse;

namespace ProjectRimFactory.Common
{
    public class PatchOperationRTFuseInsertionHook : PatchOperationAdd
    {
        protected override bool ApplyWorker(XmlDocument xml)
        {
            return GenTypes.GetTypeInAnyAssembly("RT_Fuse.CompRTFuse") == null || base.ApplyWorker(xml);
        }
    }
}
