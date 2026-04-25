using UnityEngine;

public class Statue : Pickup
{
    public override string objectiveName  => "Lost Statue";
    public override string description => "Crazy Lore Ig";
    public override float maxTravelledDistance => 1500f;
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();

    public override void OnGrab()
    {
        pickupCollider.enabled = false;
    }
}