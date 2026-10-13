using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    // start Serialized fields
    [Header("Identity")]
    public string displayName;
    public string itemID;

    [Header("Details")]
    public string itemDescription;
    public Sprite itemIcon;

    [Header("Economy")]
    public int itemPrice;
    // end Serialized fields
}