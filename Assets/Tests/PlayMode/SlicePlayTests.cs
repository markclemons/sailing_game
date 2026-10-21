using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sailing.Tests
{
    public class SlicePlayTests
    {
        [UnityTest]
        public IEnumerator SailsGathersAndPlacesABlock()
        {
            if (File.Exists(ShipLayout.SavePath)) File.Delete(ShipLayout.SavePath);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            GameObject playerGo = GameObject.Find("PlayerShip");
            Assert.NotNull(playerGo);
            ShipController player = playerGo.GetComponent<ShipController>();
            Assert.Greater(player.Grid.Layout.Blocks.Count, 0);
            Assert.Greater(GameObject.Find("Ocean").GetComponent<MeshFilter>().sharedMesh.vertexCount, 100);

            Vector3 start = player.LogicalPosition;
            player.Sail = 1f;
            player.Helm = 0f;
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(Vector3.Distance(start, player.LogicalPosition), 1f);

            player.Teleport(new Vector3(-2f, 0f, 12.4f), 0f);
            SailInput input = playerGo.GetComponent<SailInput>();
            Cargo cargo = playerGo.GetComponent<Cargo>();
            input.Interact();
            Assert.AreEqual(4, cargo.wood);

            yield return null;
            input.ToggleBuild();
            player.Grid.GhostKind = BlockKind.Hull;
            player.Grid.CursorX = 2;
            player.Grid.CursorZ = 0;
            int before = player.Grid.Layout.Blocks.Count;
            yield return null;
            input.Interact();
            Assert.AreEqual(before + 1, player.Grid.Layout.Blocks.Count);
            Assert.AreEqual(0, cargo.wood);

            player.Grid.persistent = false;
            if (File.Exists(ShipLayout.SavePath)) File.Delete(ShipLayout.SavePath);
        }
    }
}
