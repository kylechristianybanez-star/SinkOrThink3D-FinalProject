using UnityEngine;

/// <summary>
/// Attached to the docked rowboat / dock return marker on an island.
/// Lets the player press F to board the boat and return to the ocean scene.
/// </summary>
public class IslandBoatReturn : IslandInteractable
{
    public int islandIndex = 0;
    public string oceanSceneName = "GameScene";
    public float interactRange = 7.5f;

    private bool isBoarding = false;

    public override KeyCode GetInteractKey()
    {
        return KeyCode.F;
    }

    public override float GetInteractRange()
    {
        return interactRange;
    }

    public override bool CanInteract()
    {
        return !isBoarding;
    }

    public override string GetPromptText()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        bool objectiveDone = islandIndex >= 0 && islandIndex < 3 && state.completedIslandSpecialChest[islandIndex];

        if (objectiveDone && islandIndex < 2)
            return "Press F to Board Boat — Sail to " + state.GetIslandDisplayName(islandIndex + 1) + "!";

        if (objectiveDone && islandIndex == 2)
            return "Press F to Board Boat (Return to Ocean)";

        return "Press F to Board Boat (Unlock Special Chest first to advance)";
    }

    public override void Interact(IslandPlayerController player)
    {
        if (isBoarding)
            return;

        isBoarding = true;

        if (player != null)
            player.SetControlsEnabled(false);

        GameStateManager state = GameStateManager.EnsureInstance();
        state.returningFromIsland = true;
        bool advanced = state.TryAdvanceToNextIsland();

        string bannerMsg = advanced
            ? "Setting Sail for " + state.GetIslandDisplayName(state.currentIslandIndex) + "!"
            : "Boarding Boat — Returning to Ocean";

        ScreenFader fader = ScreenFader.EnsureInstance();
        fader.ShowArrivalBanner(bannerMsg, 2.2f);
        fader.FadeAndLoadScene(oceanSceneName, 0.7f);
    }
}
