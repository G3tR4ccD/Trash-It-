using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Scriptable Objects/RecipeData")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public string recipeDescription;
    public ItemData[] ingredients;
    public int ingredientsCount;
    public ItemData resultItem;
    public int resultItemCount;
}
