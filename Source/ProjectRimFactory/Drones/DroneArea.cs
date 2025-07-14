using UnityEngine;
using Verse;

namespace ProjectRimFactory.Drones;

/// <summary>
/// This is basically a clone of Area_Allowed it was created since the former one is limited to 10 in vanilla RimWorld
/// </summary>
public class DroneArea : Area
{
    private string labelInt;
    
    /// <summary>
    /// Required for ExposeData
    /// </summary>
    public DroneArea() {}
    public DroneArea(AreaManager areaManager, string label = null) : base(areaManager)
    {
        base.areaManager = areaManager;
        if (!label.NullOrEmpty())
        {
            labelInt = label;
        }
        else
        {
            int num = 1;
            while (true)
            {
                labelInt = "AreaDefaultLabel".Translate(num);
                if (areaManager.GetLabeled(labelInt) == null)
                {
                    break;
                }
                num++;
            }
        }
        colorInt = new Color(Rand.Value, Rand.Value, Rand.Value);
        colorInt = Color.Lerp(colorInt, Color.gray, 0.5f);
    }
    
    private Color colorInt;
    
    public override string Label => "DroneZone";

    public override Color Color => colorInt;

    public override int ListPriority => 3000;

    private bool mutable = false;

    public override bool Mutable => mutable;

    public void SetMutable(bool val)
    {
        mutable = val;
    }

    public override string GetUniqueLoadID()
    {
        return "Area_" + ID + "_DroneArea";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref labelInt, "label");
        Scribe_Values.Look(ref colorInt, "color");
    }

}