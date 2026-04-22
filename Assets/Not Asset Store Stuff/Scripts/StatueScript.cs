using UnityEngine;

public class Statue : Pickup
{
    public override string objectiveName  => "Lost Statue";
    public override string description => "Crazy Lore Ig";
    public override float maxTravelledDistance => 1500f;
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();
    private ChargeMeter ChargeMeter;

    public override void OnPickedUp()
    {
        pickupCollider.enabled = false;
        ChargeMeter = GameObject.FindGameObjectWithTag("ChargeMeter").GetComponent<ChargeMeter>();
        ChargeMeter.maxCharge = maxTravelledDistance;
        ChargeMeter.pickup = this;
    }
}