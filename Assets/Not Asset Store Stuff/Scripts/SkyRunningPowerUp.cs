using UnityEngine;

public class SkyRunningPowerUp : PowerUp
{
    public override string powerupName => "Skyrunning";
    protected override float powerupTime => 15f;

    protected override SphereCollider pickupCollider => GetComponent<SphereCollider>();

    public override void OnPickedUp()
    {
        _controller.SkyRunningStart();
    }

    public override void OnEnd()
    {
        _controller.SkyRunningEnd();
    }

}
