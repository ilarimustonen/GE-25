using UnityEngine;

public class CursedBucket : Bucket
{
    public override int fillVolume { get => 666; }
    public override int volume { get => 9999; }
    public override Liquid liquid { get => Liquid.Curse; }
    protected override void Pour(int amount)
    {
        Debug.Log("You cursed the land with the liquid");
    }
}
