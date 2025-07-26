using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    [StaticConstructorOnStartup]
    public class CompPRFHelp : ThingComp
    {
        private static readonly Texture2D LaunchReportTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true);
        public string HelpText => $"{parent.def.defName}_HelpText".TryTranslate(out var text) ? text : null;

        public string OrdoText => $"{parent.def.defName}_OrdoText".TryTranslate(out var text) ? text : null;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            var helpText = HelpText;
            if (!string.IsNullOrEmpty(helpText))
            {
                yield return new Command_Action
                {
                    defaultLabel = "PRFHelp".Translate(),
                    defaultDesc = "PRFHelpDesc".Translate(),
                    icon = LaunchReportTex,
                    action = () =>
                    {
                        if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() == null)
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox(helpText));
                        }
                    }
                };
            }

            if (PRFDefOf.PRFOrdoDataRummaging?.IsFinished != true) yield break; // == comparison between bool? and bool
            var ordoText = OrdoText;
            if (!string.IsNullOrEmpty(ordoText))
            {
                yield return new Command_Action
                {
                    defaultLabel = "PRFViewOrdo".Translate(parent.LabelCapNoCount),
                    icon = LaunchReportTex,
                    action = () =>
                    {
                        if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() == null)
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox(ordoText));
                        }
                    }
                };
            }
        }
    }
}
