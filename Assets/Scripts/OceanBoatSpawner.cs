using UnityEngine;

/// <summary>
/// Spawns the ocean boat beside the matching island dock when returning from IslandScene,
/// restoring movement and camera orientation cleanly.
/// </summary>
public class OceanBoatSpawner : MonoBehaviour
{
    void Start()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (!state.returningFromIsland)
            return;

        state.returningFromIsland = false;

        BoatDocking[] docks = FindObjectsByType<BoatDocking>();
        BoatDocking matchingDock = null;

        for (int i = 0; i < docks.Length; i++)
        {
            if (docks[i] != null && docks[i].islandIndex == state.lastDockedIslandIndex)
            {
                matchingDock = docks[i];
                break;
            }
        }

        if (matchingDock != null)
        {
            Transform spawnPoint = matchingDock.boatReturnSpawnPoint != null
                ? matchingDock.boatReturnSpawnPoint
                : matchingDock.dockParkingPoint;

            if (spawnPoint != null)
            {
                Vector3 pos = new Vector3(spawnPoint.position.x, transform.position.y, spawnPoint.position.z);
                Quaternion rot = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = pos;
                    rb.rotation = rot;
                }

                transform.position = pos;
                transform.rotation = rot;

                BoatMovement movement = GetComponent<BoatMovement>();
                if (movement != null)
                    movement.SyncBaseTransform();
            }
        }

        string fromIsland = state.GetIslandDisplayName(state.lastDockedIslandIndex);
        string nextIsland = state.GetIslandDisplayName(state.currentIslandIndex);
        string bannerMsg = state.lastDockedIslandIndex == state.currentIslandIndex
            ? "Returned to Ocean near " + fromIsland
            : "Sailing from " + fromIsland + " — Next Destination: " + nextIsland;

        ScreenFader.EnsureInstance().ShowArrivalBanner(bannerMsg, 2.8f);
    }
}
