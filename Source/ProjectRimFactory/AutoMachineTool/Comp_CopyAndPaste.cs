﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public class Comp_CopyAndPaste : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            return base.CompGetGizmosExtra().Concat(
                Option(parent as IStorageSetting)
                    .Select(s => s.StorageSettings)
                    .Select(x => StorageSettingsClipboard.CopyPasteGizmosFor(x))
                    .GetOrDefault(Enumerable.Empty<Gizmo>()));
        }
    }

    public interface IStorageSetting
    {
        StorageSettings StorageSettings { get; }
    }
}