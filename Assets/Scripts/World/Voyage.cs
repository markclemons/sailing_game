using UnityEngine;

namespace Sailing
{
    public class Voyage : MonoBehaviour
    {
        public ShipController playerShip;
        public ShipGrid playerGrid;
        public Cargo cargo;
        public Broadside playerGuns;
        public VoyageHud hud;
        public SailInput input;
        public Vector3 beach = new Vector3(0f, 0f, 22f);
        public float beachYaw = 40f;
        public ShipController npcShip;
        public ShipGrid npcGrid;
        public Vector3 npcHome = new Vector3(32f, 0f, 10f);

        string flash = "Sail the wind. Gather ashore. Tab builds.";
        float flashUntil = 4f;

        public void Flash(string message)
        {
            flash = message;
            flashUntil = Time.unscaledTime + 2.6f;
        }

        public void HullLost(ShipGrid grid)
        {
            if (grid == playerGrid)
            {
                int wood = grid.Layout.WreckWood;
                int salvage = grid.Layout.WreckSalvage;
                for (int i = 0; i < grid.Layout.Blocks.Count; i++)
                {
                    BlockRecord block = grid.Layout.Blocks[i];
                    if (!block.paid) continue;
                    ShipLayout.Cost(block.Kind, out int blockWood, out int blockSalvage);
                    wood += blockWood;
                    salvage += blockSalvage;
                }
                cargo.wood += wood;
                cargo.salvage += salvage;
                playerGrid.Replace(ShipLayout.Starter());
                playerShip.Teleport(beach, beachYaw);
                if (input != null && input.Building) input.ToggleBuild();
                Flash("The hull is gone. Timber washed up on the beach.");
            }
            else if (grid == npcGrid)
            {
                npcGrid.Replace(ShipLayout.Sloop());
                npcShip.Teleport(npcHome, 90f);
            }
        }

        void Update()
        {
            if (hud == null || playerShip == null || cargo == null) return;
            Vector3 wind = Wind.Direction(Time.time);
            float degrees = Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg;
            if (degrees < 0f) degrees += 360f;
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            string windName = names[Mathf.RoundToInt(degrees / 45f) % 8];
            string guns;
            if (playerGrid == null || playerGrid.Layout == null || !playerGrid.Layout.HasCannon)
                guns = "Guns: fit a cannon";
            else if (playerGuns != null && playerGuns.CooldownLeft > 0.05f)
                guns = "Guns: cooling";
            else
                guns = "Guns: ready when alongside";
            string mode = input != null && input.Building
                ? "Building " + playerGrid.GhostKind.ToString().ToLower() + "    stick or WASD moves the ghost"
                : "Sailing";
            string message = flashUntil > Time.unscaledTime ? flash : "Q/E turns the view. Come alongside to trade a slow broadside.";
            hud.Show(
                "Wood " + cargo.wood + "     Salvage " + cargo.salvage + "\n" +
                "Sail " + Mathf.RoundToInt(playerShip.Sail * 100f) + "%     Wind " + windName + "\n" +
                guns + "\n" +
                mode + "\n" +
                message);
        }
    }
}
