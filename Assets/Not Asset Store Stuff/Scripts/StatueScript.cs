using UnityEngine;

public class Statue : Pickup
{
    public override string objectiveName  => "Lost Statue";
    public override string description => "Crazy Lore Ig";
    protected override GameObject Prefab => prefab;
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();

    [SerializeField] private GameObject prefab;

    protected override string itemTag => "ITEM1";

    public override void OnPickedUp()
    {

    }

}