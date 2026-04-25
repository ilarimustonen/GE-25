using UnityEngine;

public class Star : Pickup
{
    public override string objectiveName => "Fallen Star";
    public override string description => "Crazy Lore Ig";
    public override float maxTravelledDistance => 2000f;
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();


    public override void OnGrab()
    {
        pickupCollider.enabled = false;
    }

}