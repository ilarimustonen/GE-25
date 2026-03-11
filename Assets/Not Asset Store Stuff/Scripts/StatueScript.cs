using UnityEngine;

public class Statue : Pickup
{
    public override string objectiveName  => "Lost Statue";
    public override string description => "Crazy Lore Ig";
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();

    public override void OnPickedUp()
    {
        deliveryObj = GameObject.FindGameObjectWithTag("NPC");
    }
}