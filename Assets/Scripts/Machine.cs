using UnityEngine;
using System.Collections;

public class Machine : MonoBehaviour, IInteractable
{
    [SerializeField] private Inventory hopperInput;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private MachineData machine;

    enum MachineState
    {
        Idle,
        Processing,
        Finished
    }
    MachineState currentState = MachineState.Idle;
    RecipeData currentRecipe;
    int pendingOutput;
    public bool autoProcess = false;
    public Inventory outputDestination;

    void Update()
    {
        if (!autoProcess) return;

        if (currentState == MachineState.Idle)
        {
            currentRecipe = FindAffordableRecipe();
            if (currentRecipe == null) return;

            foreach (ItemData ingredient in currentRecipe.ingredients)
            {
                hopperInput.RemoveItem(ingredient, currentRecipe.ingredientsCount);
            }

            StartCoroutine(ProcessRoutine());
            currentState = MachineState.Processing;
        }
        else if (currentState == MachineState.Finished)
        {
            Inventory outputTarget = outputDestination != null ? outputDestination : playerInventory;
            int added = outputTarget.InsertItem(currentRecipe.resultItem, pendingOutput);
            pendingOutput -= added;

            if (pendingOutput <= 0)
            {
                currentState = MachineState.Idle;
                currentRecipe = null;
            }
        }
    }

    public string GetDisplayName()
    {
        return $"{machine.displayName}";
    }
    public void Interact()
    {
        switch (currentState)
        {
            case MachineState.Idle:
                currentRecipe = FindAffordableRecipe();
                if (currentRecipe == null)
                {
                    Debug.Log("Nothing to process.");
                    break;
                }
                foreach (ItemData ingredient in currentRecipe.ingredients)
                {
                    if (!playerInventory.RemoveItem(ingredient, currentRecipe.ingredientsCount))
                    {
                        Debug.LogWarning("Could not remove " + ingredient.displayName);
                    }
                }
                Debug.Log("Machine is idle. Starting processing...");
                StartCoroutine(ProcessRoutine());
                currentState = MachineState.Processing;
                break;
            case MachineState.Processing:
                Debug.Log("Machine is currently processing. Please wait.");
                break;
            case MachineState.Finished:
                Inventory outputTarget = outputDestination != null ? outputDestination : playerInventory;
                int added = outputTarget.InsertItem(currentRecipe.resultItem, pendingOutput);
                pendingOutput -= added;
                if (pendingOutput <= 0)
                {
                    currentState = MachineState.Idle;
                    currentRecipe = null;
                    Debug.Log("Item collected. Machine is now idle.");
                }
                else
                {
                    Debug.Log("Pockets full! " + pendingOutput + " left in the machine.");
                }
                break;
        }
    }
    IEnumerator ProcessRoutine()
    {
        float actualTime = currentRecipe.processingTime / GameManager.Instance.machineSpeedMultiplier;
        yield return new WaitForSeconds(actualTime);
        pendingOutput = currentRecipe.resultItemCount;
        currentState = MachineState.Finished;
        Debug.Log("FInished processing. You can now collect your item.");
    }
    private bool CanAfford(RecipeData recipe)
    {
        Inventory source = autoProcess ? hopperInput : playerInventory;
        foreach (ItemData ingredient in recipe.ingredients)
        {
            if (source.GetItemQuantity(ingredient) < recipe.ingredientsCount)
            {
                return false;
            }
        }
        return true;
    }
    private RecipeData FindAffordableRecipe()
    {
        foreach (RecipeData r in machine.recipes)
        {
            if (CanAfford(r))
            {
                return r;
            }
        }
        return null;
    }
}

