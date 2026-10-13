using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Scriptable Objects/RecipeData")]
public class RecipeData : ScriptableObject
{
    // start Serialized fields
    [Header("Identity")]
    public string recipeName;
    public string recipeDescription;

    [Header("Ingredients")]
    public ItemData[] ingredients;
    public int ingredientsCount;

    [Header("Result")]
    public ItemData resultItem;
    public int resultItemCount;

    [Header("Processing")]
    public float processingTime = 5f;
    // end Serialized fields
}