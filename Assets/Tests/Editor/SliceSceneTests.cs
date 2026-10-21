using NUnit.Framework;
using Sailing.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sailing.Tests
{
    public class SliceSceneTests
    {
        [Test]
        public void SceneHasTheSlice()
        {
            SliceSceneBuilder.Build();

            Assert.NotNull(GameObject.Find("Ocean"));
            Assert.AreEqual("Sailing/Ocean", GameObject.Find("Ocean").GetComponent<Renderer>().sharedMaterial.shader.name);
            Assert.NotNull(GameObject.Find("Seabed"));
            Assert.NotNull(GameObject.Find("BeachIsland").GetComponent<BoxCollider>());
            MeshCollider rock = GameObject.Find("CayRock").GetComponent<MeshCollider>();
            Assert.NotNull(rock);
            Assert.IsTrue(rock.convex);
            Assert.NotNull(GameObject.Find("Shoal").GetComponent<SphereCollider>());

            GameObject player = GameObject.Find("PlayerShip");
            Assert.IsTrue(player.GetComponentInChildren<ShipGrid>().persistent);
            Assert.NotNull(player.GetComponent<SailInput>());
            Assert.IsFalse(player.GetComponent<Broadside>().alwaysArmed);
            Assert.NotNull(player.GetComponentInChildren<WakeRibbon>());

            GameObject npc = GameObject.Find("NpcShip");
            Assert.IsTrue(npc.GetComponent<Broadside>().alwaysArmed);
            Assert.NotNull(npc.GetComponent<NpcHelmsman>());
            Assert.GreaterOrEqual(npc.GetComponent<NpcHelmsman>().points.Length, 4);

            ResourceNode[] nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int wood = 0;
            int salvage = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].kind == ResourceKind.Wood) wood++;
                else salvage++;
            }
            Assert.GreaterOrEqual(wood, 2);
            Assert.GreaterOrEqual(salvage, 2);

            Assert.NotNull(GameObject.Find("TouchControls"));
            Assert.GreaterOrEqual(GameObject.Find("TouchControls").GetComponentsInChildren<Button>(true).Length, 4);
            Assert.AreEqual(40f, GameObject.Find("Main Camera").GetComponent<IsoCamera>().pitch);
            Assert.NotNull(GameObject.Find("SeaProbe").GetComponent<ReflectionProbe>());

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            InputActionMap map = asset.FindActionMap("Sail", true);
            bool gamepad = false;
            bool keyboard = false;
            bool joystick = false;
            foreach (InputBinding binding in map.FindAction("Helm").bindings)
            {
                if (binding.path != null && binding.path.Contains("Gamepad")) gamepad = true;
                if (binding.path != null && binding.path.Contains("Keyboard")) keyboard = true;
                if (binding.path != null && binding.path.Contains("Joystick")) joystick = true;
            }
            Assert.IsTrue(gamepad && keyboard && joystick);
            Assert.NotNull(map.FindAction("Interact", true));
            Assert.NotNull(map.FindAction("Build", true));
            Assert.NotNull(map.FindAction("CameraLeft", true));
            Assert.NotNull(map.FindAction("Remove", true));
        }
    }
}
