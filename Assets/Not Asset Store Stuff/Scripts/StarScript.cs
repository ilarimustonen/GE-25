using UnityEngine;

public class Star : Pickup
{
    public override string objectiveName => "Fallen Star";
    public override string description => "Crazy Lore Ig";
    protected override GameObject Prefab => prefab;
    protected override BoxCollider pickupCollider => GetComponent<BoxCollider>();

    [SerializeField] private GameObject prefab;

    protected override string itemTag => "ITEM2";

    public override void OnPickedUp()
    {

    }

}