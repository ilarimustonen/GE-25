using UnityEngine;

public class Bucket : MonoBehaviour
{
    public virtual int volume { get; set; }
    public virtual int fillVolume { get; set; }
    public enum Liquid { Water, Milk, Oil, Curse}
    public virtual Liquid liquid {  get; set; }

    protected virtual void Pour(int amount)
    {
        fillVolume -= amount;
        Debug.Log("Poured out " +  amount + ". " + fillVolume + " " + liquid + "remains in the bucket ");
    }
}
