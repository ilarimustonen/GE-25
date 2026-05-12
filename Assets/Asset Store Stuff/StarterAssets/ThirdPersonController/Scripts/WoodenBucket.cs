using UnityEngine;

public class WoodenBucket : Bucket
{
    public override int fillVolume { get => 1000; }
    public override int volume { get => 1000; }
    public override Liquid liquid { get => Liquid.Milk; }
    protected override void Pour(int amount)
    {
        base.Pour(amount);
    }
}
