using UnityEngine;

public class Statue : Pickup
{
    public override string objectiveName  => "Lost Statue";
    public override string description => "Crazy Lore Ig";
    protected override GameObject Prefab => prefab;
    [SerializeField] private GameObject prefab;

    protected override string itemTag => "ITEM1";

    public override void OnPickedUp()
    {
        Debug.Log("Picked up item: " + '"' + objectiveName + '"');
    }

}