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
        return $"{machine.displayName}";
    }
    // end GetDisplayName

    // start Interact
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
}