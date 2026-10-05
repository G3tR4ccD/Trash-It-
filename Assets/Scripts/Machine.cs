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
                    break;
                }
                foreach (ItemData ingredient in currentRecipe.ingredients)
                {
                    if (!playerInventory.RemoveItem(ingredient, currentRecipe.ingredientsCount))
                    {
                        return;
                    }
                }
                StartCoroutine(ProcessRoutine());
                currentState = MachineState.Processing;
                break;
            case MachineState.Processing:
                break;
            case MachineState.Finished:
                Inventory outputTarget = outputDestination != null ? outputDestination : playerInventory;
                int added = outputTarget.InsertItem(currentRecipe.resultItem, pendingOutput);
                pendingOutput -= added;
                if (pendingOutput <= 0)
                {
                    currentState = MachineState.Idle;
                    currentRecipe = null;
                }
                else
                {
                    return;
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

