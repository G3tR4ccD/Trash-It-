using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MachineData", menuName = "Scriptable Objects/MachineData")]
public class MachineData : ScriptableObject
{
    public string displayName;
    public string machineID;
    public List<RecipeData> recipes;
}