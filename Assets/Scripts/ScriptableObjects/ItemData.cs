using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string displayName;
    public string itemDescription;
    public int itemPrice;
    public Sprite itemIcon;
    public string itemID;

}
