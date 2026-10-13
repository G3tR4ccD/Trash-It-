using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MachineData", menuName = "Scriptable Objects/MachineData")]
public class MachineData : ScriptableObject
{
    // start Serialized fields
    [Header("Identity")]
    public string displayName;
    public string machineID;

    [Header("Recipes")]
    public List<RecipeData> recipes;
    // end Serialized fields
}