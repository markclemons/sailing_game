using UnityEngine;

namespace Sailing
{
    public class ShipGrid : MonoBehaviour
    {
        public ShipLayout Layout = new ShipLayout();
        public bool persistent;
        public Color tint = Color.white;
        public Voyage voyage;
        public Material hullMaterial;
        public Material deckMaterial;
        public Material mastMaterial;
        public Material cannonMaterial;
        public Material ghostMaterial;

        public BlockKind GhostKind = BlockKind.Hull;
        public int CursorX;
        public int CursorZ = 2;
        public bool ShowGhost;

        Material hullMat;
        Material deckMat;
        Material mastMat;
        Material cannonMat;
        Material ghostMat;
        Transform ghost;

        void Awake()
        {
            if (persistent)
            {
                ShipLayout loaded = ShipLayout.LoadFrom(ShipLayout.SavePath);
                Layout = loaded ?? ShipLayout.Starter();
            }
            else if (Layout == null || Layout.Blocks.Count == 0)
            {
                Layout = ShipLayout.Sloop();
            }
            MakeMaterials();
            Rebuild();
        }

        void OnDestroy()
        {
            DestroyMaterial(hullMat);
            DestroyMaterial(deckMat);
            DestroyMaterial(mastMat);
            DestroyMaterial(cannonMat);
            DestroyMaterial(ghostMat);
        }

        void OnApplicationQuit()
        {
            if (persistent && Application.isPlaying) Save();
        }

        public void Save()
        {
            if (!persistent || Layout == null) return;
            Layout.SaveTo(ShipLayout.SavePath);
        }

        public void Cycle(int direction)
        {
            int next = ((int)GhostKind + direction) % 4;
            if (next < 0) next += 4;
            GhostKind = (BlockKind)next;
            RefreshGhost();
        }

        public bool Place(ref int wood, ref int salvage, out string reason)
        {
            int y;
            if (!Layout.TryResolve(CursorX, ShipLayout.PreferredY(GhostKind), CursorZ, out y))
            {
                reason = "Can't attach that there.";
                return false;
            }
            if (!Layout.TryPlace(CursorX, y, CursorZ, GhostKind, ref wood, ref salvage, out reason))
                return false;
            Rebuild();
            Save();
            return true;
        }

        public bool Remove(ref int wood, ref int salvage, out string reason)
        {
            int y = ShipLayout.PreferredY(GhostKind);
            if (!Layout.TryRemove(CursorX, y, CursorZ, ref wood, ref salvage, out reason))
                return false;
            Rebuild();
            Save();
            return true;
        }

        public void ApplyDamage(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (!Layout.DamageAt(local, 8f)) return;
            Rebuild();
            if (persistent) Save();
            if (!Layout.HasHull && voyage != null)
                voyage.HullLost(this);
        }

        public void Replace(ShipLayout layout)
        {
            Layout = layout;
            Rebuild();
            if (persistent) Save();
        }

        public void MoveCursor(int dx, int dz)
        {
            CursorX = Mathf.Clamp(CursorX + dx, ShipLayout.MinX, ShipLayout.MaxX);
            CursorZ = Mathf.Clamp(CursorZ + dz, ShipLayout.MinZ, ShipLayout.MaxZ);
            RefreshGhost();
        }

        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Ghost") continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            MakeMaterials();
            for (int i = 0; i < Layout.Blocks.Count; i++)
                Spawn(Layout.Blocks[i]);
            RefreshGhost();
        }

        void Spawn(BlockRecord block)
        {
            PrimitiveType primitive = PrimitiveType.Cube;
            Vector3 scale = Vector3.one * 0.92f;
            Material material = hullMat;
            switch (block.Kind)
            {
                case BlockKind.Deck: material = deckMat; break;
                case BlockKind.Mast: material = mastMat; break;
                case BlockKind.Cannon: material = cannonMat; break;
            }
            var go = GameObject.CreatePrimitive(primitive);
            go.name = block.Kind.ToString();
            go.transform.SetParent(transform, false);
            go.transform.localPosition = ShipLayout.Center(block);
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (block.Kind == BlockKind.Mast)
                AddSail(go.transform);
            if (block.Kind == BlockKind.Cannon)
                AddBarrel(go.transform);
        }

        void AddSail(Transform mast)
        {
            var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sail.name = "Sail";
            sail.transform.SetParent(mast, false);
            sail.transform.localPosition = new Vector3(0f, 0.15f, 0.35f);
            sail.transform.localScale = new Vector3(0.08f, 0.7f, 0.55f);
            Collider collider = sail.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            sail.GetComponent<Renderer>().sharedMaterial = deckMat;
        }

        void AddBarrel(Transform cannon)
        {
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(cannon, false);
            barrel.transform.localPosition = new Vector3(0.35f, 0.1f, 0f);
            barrel.transform.localScale = new Vector3(0.7f, 0.28f, 0.28f);
            Collider collider = barrel.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            barrel.GetComponent<Renderer>().sharedMaterial = cannonMat;
        }

        public void RefreshGhost()
        {
            if (!ShowGhost)
            {
                if (ghost != null) ghost.gameObject.SetActive(false);
                return;
            }
            if (ghost == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Ghost";
                go.transform.SetParent(transform, false);
                Collider collider = go.GetComponent<Collider>();
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
                ghost = go.transform;
            }
            ghost.gameObject.SetActive(true);
            int y;
            bool ok = Layout.TryResolve(CursorX, ShipLayout.PreferredY(GhostKind), CursorZ, out y);
            if (!ok) y = ShipLayout.PreferredY(GhostKind);
            var record = new BlockRecord { x = CursorX, y = y, z = CursorZ, kind = (int)GhostKind };
            ghost.localPosition = ShipLayout.Center(record);
            ghost.localScale = Vector3.one * 0.92f;
            if (ghostMat != null)
            {
                ghostMat.SetColor("_BaseColor", ok ? new Color(0.6f, 0.9f, 0.55f, 0.45f) : new Color(0.9f, 0.35f, 0.3f, 0.45f));
                ghost.GetComponent<Renderer>().sharedMaterial = ghostMat;
            }
        }

        void MakeMaterials()
        {
            if (hullMat != null) return;
            hullMat = Tint(hullMaterial, new Color(0.45f, 0.28f, 0.14f));
            deckMat = Tint(deckMaterial, new Color(0.76f, 0.58f, 0.34f));
            mastMat = Tint(mastMaterial, new Color(0.55f, 0.4f, 0.22f));
            cannonMat = Tint(cannonMaterial, new Color(0.22f, 0.24f, 0.28f));
            ghostMat = ghostMaterial != null ? new Material(ghostMaterial) : null;
        }

        Material Tint(Material source, Color color)
        {
            color *= tint;
            color.a = 1f;
            Material material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            return material;
        }

        static void DestroyMaterial(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
