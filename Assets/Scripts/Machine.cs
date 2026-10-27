using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour, IInteractable
{
    // start Serialized fields
    [Header("References")]
    [SerializeField] private Inventory hopperInput;
    [SerializeField] private Inventory playerInventory;
    public Inventory outputDestination;
    public string machineID;

    [Header("Machine")]
    [SerializeField] private MachineData machine;

    [Header("Settings")]
    public bool autoProcess = false;
    private bool waitForCollection = true;
    // end Serialized fields

    // start MachineState enum
    enum MachineState
    {
        Idle,
        Processing,
        Finished
    }
    // end MachineState enum

    // start Runtime state
    MachineState currentState = MachineState.Idle;
    RecipeData currentRecipe;
    int pendingOutput;
    private float timeRemaining;
    public bool HasOutput => currentState == MachineState.Finished;
    public bool HasWork => currentState != MachineState.Idle || FindAffordableRecipe() != null;
    private MonoBehaviour worker;
    // end Runtime state

    // start Update
    void Update()
    {
        if (currentState == MachineState.Processing)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                pendingOutput = currentRecipe.resultItemCount;
                currentState = MachineState.Finished;
            }
        }

        if (!autoProcess) return;

        if (currentState == MachineState.Idle)
        {
            currentRecipe = FindAffordableRecipe();
            if (currentRecipe == null) return;

            foreach (ItemData ingredient in currentRecipe.ingredients)
            {
                hopperInput.RemoveItem(ingredient, currentRecipe.ingredientsCount);
            }

            StartProcessing();

        }
        else if (currentState == MachineState.Finished && !waitForCollection)
        {
            TryDeliverOutput();
        }
    }
    // end Update

    // start GetDisplayName
    public string GetDisplayName()
    {
        switch (currentState)
        {
            case MachineState.Processing:
                return $"{machine.displayName} ({Mathf.CeilToInt(timeRemaining)}s)\n{DescribeRecipe(currentRecipe)}";

            case MachineState.Finished:
                return $"{machine.displayName} (done)";

            default:
                string text = machine.displayName;
                foreach (RecipeData r in machine.recipes)
                {
                    text += "\n" + DescribeRecipe(r);
                }
                return text;
        }
    }
    // end GetDisplayName

    // start DescribeRecipe
    string DescribeRecipe(RecipeData r)
    {
        List<string> parts = new List<string>();
        foreach (ItemData ingredient in r.ingredients)
        {
            parts.Add($"{r.ingredientsCount} {ingredient.displayName}");
        }

        float seconds = r.processingTime / GameManager.Instance.machineSpeedMultiplier;
        return $"{string.Join(" + ", parts)} -> {r.resultItemCount} {r.resultItem.displayName} ({seconds:0.#}s)";
    }
    // end DescribeRecipe

    // start Interact
    public void Interact()
    {
        if (autoProcess)
        {
            InteractAsCarrier();
            return;
        }
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
                StartProcessing();

                break;
            case MachineState.Processing:
                break;
            case MachineState.Finished:
                TryDeliverOutput();
                break;
        }
    }
    // end Interact

    // start InteractAsCarrier
    void InteractAsCarrier()
    {
        if (currentState == MachineState.Finished)
        {
            TakeOutput(playerInventory);
            return;
        }

        var carried = new List<KeyValuePair<ItemData, int>>(playerInventory.GetAllItems());
        foreach (var entry in carried)
        {
            if (!UsesIngredient(entry.Key)) continue;

            int inserted = hopperInput.InsertItem(entry.Key, entry.Value);
            playerInventory.RemoveItem(entry.Key, inserted);
        }
    }
    // end InteractAsCarrier

    // start UsesIngredient
    bool UsesIngredient(ItemData item)
    {
        foreach (RecipeData r in machine.recipes)
        {
            foreach (ItemData ingredient in r.ingredients)
            {
                if (ingredient == item) return true;
            }
        }
        return false;
    }
    // end UsesIngredient

    // start CanAfford
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
    // end CanAfford

    // start FindAffordableRecipe
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
    // end FindAffordableRecipe

    // start StartProcessing
    void StartProcessing()
    {
        timeRemaining = currentRecipe.processingTime / GameManager.Instance.machineSpeedMultiplier;
        currentState = MachineState.Processing;

    }
    // end StartProcessing

    // start TakeOutput
    public int TakeOutput(Inventory target)
    {
        if (currentState != MachineState.Finished)
        {
            return 0;
        }

        int added = target.InsertItem(currentRecipe.resultItem, pendingOutput);
        pendingOutput -= added;
        if (pendingOutput <= 0)
        {
            currentState = MachineState.Idle;
            currentRecipe = null;
        }
        return added;
    }
    // end TakeOutput

    // start TryDeliverOutput
    void TryDeliverOutput()
    {
        Inventory outputTarget = outputDestination != null ? outputDestination : playerInventory;
        TakeOutput(outputTarget);
    }
    // end TryDeliverOutput

    // start GetSaveEntry
    public MachineSaveEntry GetSaveEntry()
    {
        MachineSaveEntry entry = new MachineSaveEntry();
        entry.machineID = machineID;
        entry.machineState = (int)currentState;
        entry.timeRemaining = timeRemaining;
        entry.pendingOutput = pendingOutput;
        entry.recipeIndex = machine.recipes.IndexOf(currentRecipe);
        return entry;
    }
    // end GetSaveEntry


    // start LoadFromEntry
    public void LoadFromEntry(MachineSaveEntry entry)
    {
        currentState = (MachineState)entry.machineState;
        timeRemaining = entry.timeRemaining;
        pendingOutput = entry.pendingOutput;

        if (entry.recipeIndex >= 0 && entry.recipeIndex < machine.recipes.Count)
        {
            currentRecipe = machine.recipes[entry.recipeIndex];
        }
        else
        {
            currentRecipe = null;
            currentState = MachineState.Idle;
        }
    }
    // end LoadFromEntry

    // start TryStaff
    public bool TryStaff(MonoBehaviour who)
    {
        if (worker == null || worker == who)
        {
            worker = who;
            return true;
        }
        return false;
    }
    // end TryStaff

    // start Unstaff
    public void Unstaff(MonoBehaviour who)
    {
        if (worker == who)
        {
            worker = null;
        }
    }
    // end Unstaff

}