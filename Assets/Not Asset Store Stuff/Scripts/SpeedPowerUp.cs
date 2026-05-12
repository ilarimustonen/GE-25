using UnityEngine;

public class SpeedPowerUp : PowerUp
{
    public override string powerupName => "Speed";
    protected override float powerupTime => 15f;

    protected override SphereCollider pickupCollider => GetComponent<SphereCollider>();

    public override void OnPickedUp()
    {
        _controller.SpeedUpStart();
    }

    public override void OnEnd()
    {
        _controller.SpeedUpEnd();
    }

}
