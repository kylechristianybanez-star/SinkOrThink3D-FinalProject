using UnityEngine;

/// <summary>
/// Base class for interactive objects on islands (chests, docked boat return, repair station).
/// </summary>
public class IslandInteractable : MonoBehaviour
{
    public virtual string GetPromptText()
    {
        return "Press E to Interact";
    }

    public virtual KeyCode GetInteractKey()
    {
        return KeyCode.E;
    }

    public virtual bool CanInteract()
    {
        return true;
    }

    public virtual float GetInteractRange()
    {
        return 4.2f;
    }

    public virtual void Interact(IslandPlayerController player)
    {
    }
}
