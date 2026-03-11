using UnityEngine;

public class Star : Pickup
{
    public override string objectiveName => "Fallen Star";
    public override string description => "Crazy Lore Ig";
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();


    public override void OnPickedUp()
    {
        deliveryObj = GameObject.FindGameObjectWithTag("NPC");
    }

}