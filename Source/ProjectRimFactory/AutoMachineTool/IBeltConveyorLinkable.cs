using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    interface IBeltConveyorLinkable : ProjectRimFactory.Common.IPRF_Building
    {
        void Link(IBeltConveyorLinkable linkable);
        void Unlink(IBeltConveyorLinkable linkable);
        Rot4 Rotation { get; }
        IntVec3 Position { get; }
        bool ReceivableNow(bool underground, Thing thing);
        bool CanSendToLevel(ConveyorLevel level);
        bool CanReceiveFromLevel(ConveyorLevel level);
        bool IsUnderground { get; }
        IEnumerable<Rot4> OutputRots { get; }
        bool IsStuck { get; }
    }
    public enum ConveyorLevel {
        Underground=-1,
        Ground=0,
    }
}
