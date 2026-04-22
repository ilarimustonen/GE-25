using UnityEngine;

public class Star : Pickup
{
    public override string objectiveName => "Fallen Star";
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