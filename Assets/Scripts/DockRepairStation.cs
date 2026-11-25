using UnityEngine;

/// <summary>
/// Interactive dock repair workbench where players can spend 10 Coins to restore +20 Boat HP.
/// </summary>
public class DockRepairStation : IslandInteractable
{
    public ParticleSystem repairBurstParticles;
    public float interactRange = 4.2f;

    public override KeyCode GetInteractKey()
    {
        return KeyCode.E;
    }

    public override float GetInteractRange()
    {
        return interactRange;
    }

    public override string GetPromptText()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.boatHealth >= state.maxBoatHealth)
            return "Boat Repair Bench — Boat Health is Full (" + state.boatHealth + "/" + state.maxBoatHealth + ")";

        return "Press E to Repair Boat (" + GameStateManager.CoinRepairCost + " Coins = +" +
               GameStateManager.CoinRepairHealAmount + " HP | Your Coins: " + state.coins + ")";
    }

    public override void Interact(IslandPlayerController player)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        bool repaired = state.TryCoinRepair(out _, out string feedback);

        if (repaired)
        {
            GameAudioManager.EnsureInstance().PlayRepair();
            ScreenFeedbackFX.EnsureInstance().TriggerRepairFeedback();
            if (repairBurstParticles != null)
                repairBurstParticles.Play();
        }

        ScreenFader.EnsureInstance().ShowArrivalBanner(feedback, 2.4f);
    }
}
