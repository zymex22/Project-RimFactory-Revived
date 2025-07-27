using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones;

/// <summary>
/// This Class is used for the Area Selection for Drones where the range is unlimited (0)
/// </summary>
public class DroneAreaSelector : Designator
{
    //Content is mostly a copy of Designator_AreaAllowedExpand

    private static Area selectedArea;

    public Action<Area> SelectAction;

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        return loc.InBounds(Map) && Designator_AreaAllowed.SelectedArea != null && !Designator_AreaAllowed.SelectedArea[loc];
    }
    public override void SelectedUpdate() {}

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract()) return;
        if (selectedArea != null)
        {
            //base.ProcessInput(ev);
        }
        AreaUtility.MakeAllowedAreaListFloatMenu(delegate (Area a)
        {
            selectedArea = a;
            SelectAction(selectedArea);

        }, addNullAreaOption: true, addManageOption: false, Map);
    }

}