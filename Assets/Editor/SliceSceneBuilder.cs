using System.IO;
using Sailing;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sailing.EditorTools
{
    public static class SliceSceneBuilder
    {
        [MenuItem("Sailing/Build Slice Scene")]
        public static void Build()
        {
            Shader oceanShader = Shader.Find("Sailing/Ocean");
            Shader seabedShader = Shader.Find("Sailing/Seabed");
            Shader wakeShader = Shader.Find("Sailing/Wake");
            Shader ghostShader = Shader.Find("Sailing/Ghost");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (oceanShader == null || seabedShader == null || wakeShader == null || ghostShader == null || lit == null)
                throw new System.InvalidOperationException("A sailing shader failed to import.");

            Directory.CreateDirectory("Assets/Materials");
            Material ocean = Save(new Material(oceanShader), "Assets/Materials/Ocean.mat");
            Material wake = Save(new Material(wakeShader), "Assets/Materials/Wake.mat");
            Material seabed = Save(new Material(seabedShader) { color = new Color(0.52f, 0.62f, 0.45f) }, "Assets/Materials/Seabed.mat");
            seabed.SetColor("_Color", new Color(0.52f, 0.62f, 0.45f));
            Material sand = Lit(lit, "Assets/Materials/Sand.mat", new Color(0.86f, 0.75f, 0.48f));
            Material leaf = Lit(lit, "Assets/Materials/Leaf.mat", new Color(0.16f, 0.48f, 0.28f));
            Material trunk = Lit(lit, "Assets/Materials/Trunk.mat", new Color(0.38f, 0.24f, 0.12f));
            Material wood = Lit(lit, "Assets/Materials/Wood.mat", new Color(0.55f, 0.36f, 0.18f));
            Material salvage = Lit(lit, "Assets/Materials/Salvage.mat", new Color(0.72f, 0.7f, 0.6f));
            Material rock = Lit(lit, "Assets/Materials/Rock.mat", new Color(0.45f, 0.42f, 0.38f));
            Material hull = Lit(lit, "Assets/Materials/Hull.mat", new Color(0.45f, 0.28f, 0.14f));
            Material deck = Lit(lit, "Assets/Materials/Deck.mat", new Color(0.76f, 0.58f, 0.34f));
            Material mast = Lit(lit, "Assets/Materials/Mast.mat", new Color(0.55f, 0.4f, 0.22f));
            Material cannon = Lit(lit, "Assets/Materials/Cannon.mat", new Color(0.22f, 0.24f, 0.28f));
            Material ghost = Save(new Material(ghostShader), "Assets/Materials/Ghost.mat");
            Material sky = Save(new Material(Shader.Find("Skybox/Procedural")), "Assets/Materials/Sky.mat");
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_AtmosphereThickness", 0.9f);
            sky.SetColor("_SkyTint", new Color(0.42f, 0.62f, 0.85f));
            sky.SetColor("_GroundColor", new Color(0.32f, 0.42f, 0.4f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.74f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.7f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.8f, 0.84f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 240f;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.82f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var probe = new GameObject("SeaProbe").AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(500f, 80f, 500f);
            probe.resolution = 128;
            probe.transform.position = new Vector3(40f, 8f, 20f);

            var seaBed = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seaBed.name = "Seabed";
            seaBed.transform.position = new Vector3(40f, -2.6f, 20f);
            seaBed.transform.localScale = new Vector3(50f, 1f, 50f);
            seaBed.GetComponent<Renderer>().sharedMaterial = seabed;

            var oceanGo = new GameObject("Ocean");
            oceanGo.AddComponent<MeshFilter>();
            oceanGo.AddComponent<MeshRenderer>().sharedMaterial = ocean;
            oceanGo.AddComponent<OceanSurface>();

            var archipelago = new GameObject("Archipelago");
            var beach = new GameObject("BeachIsland");
            beach.transform.SetParent(archipelago.transform, false);
            beach.transform.position = new Vector3(0f, 0.3f, 0f);
            beach.AddComponent<BoxCollider>().size = new Vector3(24f, 2.2f, 16f);
            var sandMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandMesh.name = "Sand";
            sandMesh.transform.SetParent(beach.transform, false);
            sandMesh.transform.localScale = new Vector3(24f, 2.2f, 16f);
            Object.DestroyImmediate(sandMesh.GetComponent<Collider>());
            sandMesh.GetComponent<Renderer>().sharedMaterial = sand;
            Palm(beach.transform, new Vector3(-4f, 1.1f, -1f), trunk, leaf);
            Palm(beach.transform, new Vector3(3.5f, 1.1f, 1.5f), trunk, leaf);
            Vane(beach.transform, new Vector3(-6f, 1.1f, 2f), deck);

            var cay = new GameObject("Cay");
            cay.transform.SetParent(archipelago.transform, false);
            cay.transform.position = new Vector3(86f, 0.25f, 38f);
            cay.AddComponent<BoxCollider>().size = new Vector3(16f, 2f, 12f);
            var caySand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caySand.name = "Sand";
            caySand.transform.SetParent(cay.transform, false);
            caySand.transform.localScale = new Vector3(16f, 2f, 12f);
            Object.DestroyImmediate(caySand.GetComponent<Collider>());
            caySand.GetComponent<Renderer>().sharedMaterial = sand;
            Palm(cay.transform, new Vector3(1f, 1f, 0.5f), trunk, leaf);
            Rock(cay.transform, rock);

            var shoal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shoal.name = "Shoal";
            shoal.transform.SetParent(archipelago.transform, false);
            shoal.transform.position = new Vector3(48f, -0.45f, 14f);
            shoal.transform.localScale = new Vector3(14f, 0.9f, 10f);
            shoal.GetComponent<Renderer>().sharedMaterial = seabed;

            Node("BeachWood", new Vector3(-2f, 0.35f, 10.6f), ResourceKind.Wood, 4, wood);
            Node("BeachWoodEast", new Vector3(9f, 0.35f, 10.2f), ResourceKind.Wood, 4, wood);
            Node("CayWood", new Vector3(86f, 0.35f, 30f), ResourceKind.Wood, 4, wood);
            Node("ShoalSalvage", new Vector3(48f, 0.3f, 22f), ResourceKind.Salvage, 2, salvage);
            Node("ShoalSalvageWest", new Vector3(38f, 0.3f, 18f), ResourceKind.Salvage, 2, salvage);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            ShipController player = MakeShip("PlayerShip", new Vector3(0f, 0f, 22f), 40f, true, Color.white, hull, deck, mast, cannon, ghost, wake);
            ShipController npc = MakeShip("NpcShip", new Vector3(32f, 0f, 10f), 90f, false, new Color(1.25f, 0.7f, 0.62f), hull, deck, mast, cannon, ghost, null);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 450f;
            camGo.AddComponent<AudioListener>();
            UniversalAdditionalCameraData extra = cam.GetUniversalAdditionalCameraData();
            extra.requiresDepthOption = CameraOverrideOption.On;
            IsoCamera iso = camGo.AddComponent<IsoCamera>();
            iso.target = player.transform;
            iso.pitch = 40f;
            iso.distance = 36f;

            var cargo = player.gameObject.AddComponent<Cargo>();
            var hud = Hud(out Text label);
            var voyage = new GameObject("Voyage").AddComponent<Voyage>();
            var input = player.gameObject.AddComponent<SailInput>();
            TouchHelm stick = TouchUi(input, out GameObject touchRoot);
            input.actions = actions;
            input.ship = player;
            input.iso = iso;
            input.grid = player.Grid;
            input.cargo = cargo;
            input.voyage = voyage;
            input.touch = stick;

            Broadside playerGuns = player.gameObject.AddComponent<Broadside>();
            Broadside npcGuns = npc.gameObject.AddComponent<Broadside>();
            playerGuns.ship = player;
            playerGuns.grid = player.Grid;
            playerGuns.opponent = npcGuns;
            npcGuns.ship = npc;
            npcGuns.grid = npc.Grid;
            npcGuns.opponent = playerGuns;
            npcGuns.alwaysArmed = true;

            NpcHelmsman helm = npc.gameObject.AddComponent<NpcHelmsman>();
            helm.ship = npc;
            helm.points = new[]
            {
                new Vector3(32f, 0f, 10f),
                new Vector3(72f, 0f, 10f),
                new Vector3(72f, 0f, 28f),
                new Vector3(34f, 0f, 28f),
            };

            voyage.playerShip = player;
            voyage.playerGrid = player.Grid;
            voyage.playerGrid.voyage = voyage;
            voyage.cargo = cargo;
            voyage.playerGuns = playerGuns;
            voyage.hud = hud;
            voyage.input = input;
            voyage.npcShip = npc;
            voyage.npcGrid = npc.Grid;
            voyage.npcGrid.voyage = voyage;
            hud.label = label;

            var events = new GameObject("EventSystem");
            var system = events.AddComponent<EventSystem>();
            system.sendNavigationEvents = false;
            var module = events.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = actions;

            PlayerSettings.companyName = "Sailing Game";
            PlayerSettings.productName = "Caribbean Sail";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.sailinggame.caribbean");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.sailinggame.caribbean");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.sailinggame.caribbean");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Caribbean slice scene saved. Touch controls: " + touchRoot.name);
        }

        static ShipController MakeShip(string name, Vector3 position, float yaw, bool player, Color tint, Material hull, Material deck, Material mast, Material cannon, Material ghost, Material wake)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var ship = go.AddComponent<ShipController>();
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(go.transform, false);
            var grid = gridGo.AddComponent<ShipGrid>();
            grid.persistent = player;
            grid.tint = tint;
            grid.hullMaterial = hull;
            grid.deckMaterial = deck;
            grid.mastMaterial = mast;
            grid.cannonMaterial = cannon;
            grid.ghostMaterial = ghost;
            ship.Grid = grid;
            if (wake != null)
            {
                var ribbon = new GameObject("Wake");
                ribbon.transform.SetParent(go.transform, false);
                ribbon.AddComponent<MeshFilter>();
                ribbon.AddComponent<MeshRenderer>().sharedMaterial = wake;
                ribbon.AddComponent<WakeRibbon>().ship = ship;
            }
            return ship;
        }

        static void Palm(Transform parent, Vector3 localPosition, Material trunk, Material leaf)
        {
            var root = new GameObject("Palm");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(root.transform, false);
            stem.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            stem.transform.localScale = new Vector3(0.28f, 1.5f, 0.28f);
            Object.DestroyImmediate(stem.GetComponent<Collider>());
            stem.GetComponent<Renderer>().sharedMaterial = trunk;
            for (int i = 0; i < 4; i++)
            {
                var frond = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                frond.transform.SetParent(root.transform, false);
                float angle = i * Mathf.PI * 0.5f;
                frond.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.75f, 3.05f, Mathf.Sin(angle) * 0.75f);
                frond.transform.localScale = new Vector3(1.15f, 0.28f, 0.5f);
                Object.DestroyImmediate(frond.GetComponent<Collider>());
                frond.GetComponent<Renderer>().sharedMaterial = leaf;
            }
        }

        static void Vane(Transform parent, Vector3 localPosition, Material cloth)
        {
            var root = new GameObject("WindVane");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.AddComponent<WindVane>();
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.transform.SetParent(root.transform, false);
            flag.transform.localPosition = new Vector3(0f, 2.2f, 0.55f);
            flag.transform.localScale = new Vector3(0.06f, 0.45f, 1.1f);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            flag.GetComponent<Renderer>().sharedMaterial = cloth;
        }

        static void Rock(Transform parent, Material material)
        {
            var mesh = new Mesh { name = "CayRock" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 2.4f, 0f),
                new Vector3(-3.1f, 0f, -1.8f),
                new Vector3(3.3f, 0.2f, -1.5f),
                new Vector3(2.1f, 0f, 2.8f),
                new Vector3(-2.2f, 0.1f, 2.5f),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 1, 2, 3, 1, 3, 4 };
            mesh.RecalculateNormals();
            string path = "Assets/Art/CayRock.asset";
            Mesh saved = Save(mesh, path);
            var go = new GameObject("CayRock");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(2.2f, 0.9f, -1.4f);
            go.AddComponent<MeshFilter>().sharedMesh = saved;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = saved;
            collider.convex = true;
        }

        static void Node(string name, Vector3 position, ResourceKind kind, int amount, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = kind == ResourceKind.Wood ? new Vector3(1.1f, 0.35f, 0.55f) : new Vector3(0.7f, 0.28f, 0.7f);
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var node = go.AddComponent<ResourceNode>();
            node.kind = kind;
            node.amount = amount;
        }

        static VoyageHud Hud(out Text label)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(760f, 210f);
            var image = panel.AddComponent<Image>();
            image.sprite = White();
            image.color = new Color(0.08f, 0.14f, 0.16f, 0.62f);
            var textGo = new GameObject("Readout");
            textGo.transform.SetParent(panel.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -12f);
            label = textGo.AddComponent<Text>();
            label.font = font;
            label.fontSize = 26;
            label.color = new Color(0.96f, 0.93f, 0.82f);
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.text = "Caribbean";
            var hud = canvasGo.AddComponent<VoyageHud>();
            return hud;
        }

        static TouchHelm TouchUi(SailInput input, out GameObject root)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite sprite = White();
            root = new GameObject("TouchControls");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            var stick = new GameObject("HelmStick");
            stick.transform.SetParent(root.transform, false);
            var stickRect = stick.AddComponent<RectTransform>();
            stickRect.anchorMin = new Vector2(0f, 0f);
            stickRect.anchorMax = new Vector2(0f, 0f);
            stickRect.pivot = new Vector2(0.5f, 0.5f);
            stickRect.anchoredPosition = new Vector2(170f, 170f);
            stickRect.sizeDelta = new Vector2(200f, 200f);
            var stickImage = stick.AddComponent<Image>();
            stickImage.sprite = sprite;
            stickImage.color = new Color(0.9f, 0.85f, 0.7f, 0.28f);
            var knobGo = new GameObject("Knob");
            knobGo.transform.SetParent(stick.transform, false);
            var knobRect = knobGo.AddComponent<RectTransform>();
            knobRect.sizeDelta = new Vector2(78f, 78f);
            var knobImage = knobGo.AddComponent<Image>();
            knobImage.sprite = sprite;
            knobImage.color = new Color(0.95f, 0.9f, 0.75f, 0.85f);
            var helm = stick.AddComponent<TouchHelm>();
            helm.knob = knobRect;
            helm.range = 70f;

            ButtonAt(root.transform, sprite, font, "Interact", new Vector2(-36f, 36f), input.Interact);
            ButtonAt(root.transform, sprite, font, "Build", new Vector2(-36f, 100f), input.ToggleBuild);
            ButtonAt(root.transform, sprite, font, "Remove", new Vector2(-200f, 36f), input.Remove);
            ButtonAt(root.transform, sprite, font, "Cam Left", new Vector2(-200f, 164f), input.CameraLeft);
            ButtonAt(root.transform, sprite, font, "Cam Right", new Vector2(-36f, 164f), input.CameraRight);
            ButtonAt(root.transform, sprite, font, "Block", new Vector2(-200f, 100f), input.CycleType);
            return helm;
        }

        static void ButtonAt(Transform parent, Sprite sprite, Font font, string caption, Vector2 anchored, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(150f, 54f);
            rect.anchoredPosition = anchored;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.86f, 0.72f, 0.42f, 0.92f);
            var button = go.AddComponent<Button>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.16f, 0.1f, 0.05f);
            text.text = caption;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        static Sprite White()
        {
            const string path = "Assets/UI/White.png";
            Directory.CreateDirectory("Assets/UI");
            if (!File.Exists(path))
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Material Lit(Shader shader, string path, Color color)
        {
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            return Save(material, path);
        }

        static T Save<T>(T asset, string path) where T : Object
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            T loaded = AssetDatabase.LoadAssetAtPath<T>(path);
            return loaded != null ? loaded : asset;
        }
    }
}
