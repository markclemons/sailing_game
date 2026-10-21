using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sailing
{
    public enum BlockKind
    {
        Hull = 0,
        Deck = 1,
        Mast = 2,
        Cannon = 3,
    }

    public enum ResourceKind
    {
        Wood = 0,
        Salvage = 1,
    }

    [Serializable]
    public class BlockRecord
    {
        public int x;
        public int y;
        public int z;
        public int kind;
        public bool paid;

        public BlockKind Kind => (BlockKind)kind;
    }

    [Serializable]
    class ShipSaveFile
    {
        public BlockRecord[] blocks;
        public int wreckWood;
        public int wreckSalvage;
    }

    public sealed class ShipLayout
    {
        public const int MinX = -2;
        public const int MaxX = 2;
        public const int MinZ = -4;
        public const int MaxZ = 3;
        public const int MinY = 0;
        public const int MaxY = 2;

        public readonly List<BlockRecord> Blocks = new List<BlockRecord>();
        public int WreckWood;
        public int WreckSalvage;

        public static string SavePath => Path.Combine(Application.persistentDataPath, "ship.json");

        public static void Cost(BlockKind kind, out int wood, out int salvage)
        {
            switch (kind)
            {
                case BlockKind.Hull: wood = 4; salvage = 0; break;
                case BlockKind.Deck: wood = 2; salvage = 0; break;
                case BlockKind.Mast: wood = 3; salvage = 1; break;
                default: wood = 2; salvage = 2; break;
            }
        }

        public static int PreferredY(BlockKind kind)
        {
            if (kind == BlockKind.Hull) return 0;
            if (kind == BlockKind.Mast) return 2;
            return 1;
        }

        public static Vector3 Center(BlockRecord block)
        {
            return new Vector3(block.x, block.y * 0.92f + 0.46f, block.z);
        }

        public static ShipLayout Starter()
        {
            var layout = new ShipLayout();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = 0; z <= 1; z++)
                    layout.Blocks.Add(new BlockRecord { x = x, y = 0, z = z, kind = (int)BlockKind.Hull, paid = false });
            }
            return layout;
        }

        public static ShipLayout Sloop()
        {
            ShipLayout layout = Starter();
            layout.Blocks.Add(new BlockRecord { x = 0, y = 1, z = 0, kind = (int)BlockKind.Deck, paid = false });
            layout.Blocks.Add(new BlockRecord { x = 0, y = 2, z = 0, kind = (int)BlockKind.Mast, paid = false });
            layout.Blocks.Add(new BlockRecord { x = 1, y = 1, z = 1, kind = (int)BlockKind.Cannon, paid = false });
            layout.Blocks.Add(new BlockRecord { x = -1, y = 1, z = 1, kind = (int)BlockKind.Cannon, paid = false });
            return layout;
        }

        public bool HasHull
        {
            get
            {
                for (int i = 0; i < Blocks.Count; i++)
                    if (Blocks[i].Kind == BlockKind.Hull) return true;
                return false;
            }
        }

        public bool HasCannon
        {
            get
            {
                for (int i = 0; i < Blocks.Count; i++)
                    if (Blocks[i].Kind == BlockKind.Cannon) return true;
                return false;
            }
        }

        public float MastBonus
        {
            get
            {
                for (int i = 0; i < Blocks.Count; i++)
                    if (Blocks[i].Kind == BlockKind.Mast) return 0.18f;
                return 0f;
            }
        }

        public float Radius
        {
            get
            {
                float max = 1.6f;
                for (int i = 0; i < Blocks.Count; i++)
                {
                    var block = Blocks[i];
                    float reach = new Vector2(block.x, block.z).magnitude + 0.8f;
                    if (reach > max) max = reach;
                }
                return max;
            }
        }

        public bool InBounds(int x, int y, int z)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY && z >= MinZ && z <= MaxZ;
        }

        public bool Occupied(int x, int y, int z)
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                BlockRecord block = Blocks[i];
                if (block.x == x && block.y == y && block.z == z) return true;
            }
            return false;
        }

        public bool Adjacent(int x, int y, int z)
        {
            if (Blocks.Count == 0) return true;
            for (int i = 0; i < Blocks.Count; i++)
            {
                BlockRecord block = Blocks[i];
                int manhattan = Mathf.Abs(block.x - x) + Mathf.Abs(block.y - y) + Mathf.Abs(block.z - z);
                if (manhattan == 1) return true;
            }
            return false;
        }

        public bool CanPlace(int x, int y, int z)
        {
            return InBounds(x, y, z) && !Occupied(x, y, z) && Adjacent(x, y, z);
        }

        public bool TryResolve(int x, int preferredY, int z, out int y)
        {
            y = Mathf.Clamp(preferredY, MinY, MaxY);
            if (CanPlace(x, y, z)) return true;
            for (int delta = 1; delta <= MaxY - MinY; delta++)
            {
                if (CanPlace(x, y + delta, z)) { y += delta; return true; }
                if (CanPlace(x, y - delta, z)) { y -= delta; return true; }
            }
            return false;
        }

        public bool TryPlace(int x, int y, int z, BlockKind kind, ref int wood, ref int salvage, out string reason)
        {
            if (!InBounds(x, y, z)) { reason = "Outside the raft."; return false; }
            if (Occupied(x, y, z)) { reason = "That cell is full."; return false; }
            if (!Adjacent(x, y, z)) { reason = "Attach it to the raft."; return false; }
            Cost(kind, out int needWood, out int needSalvage);
            if (wood < needWood || salvage < needSalvage)
            {
                reason = "Need " + needWood + " wood" + (needSalvage > 0 ? " and " + needSalvage + " salvage." : ".");
                return false;
            }
            wood -= needWood;
            salvage -= needSalvage;
            Blocks.Add(new BlockRecord { x = x, y = y, z = z, kind = (int)kind, paid = true });
            reason = null;
            return true;
        }

        public bool TryRemove(int x, int y, int z, ref int wood, ref int salvage, out string reason)
        {
            int index = Find(x, y, z);
            if (index < 0)
            {
                int highest = HighestAt(x, z);
                if (highest < 0) { reason = "Nothing there."; return false; }
                index = highest;
            }
            BlockRecord block = Blocks[index];
            if (block.Kind == BlockKind.Hull && HullCount() <= 1)
            {
                reason = "The raft needs a hull.";
                return false;
            }
            if (block.paid)
            {
                Cost(block.Kind, out int refundWood, out int refundSalvage);
                wood += refundWood;
                salvage += refundSalvage;
            }
            Blocks.RemoveAt(index);
            reason = null;
            return true;
        }

        public bool DamageAt(Vector3 localPoint, float maxDistance)
        {
            int best = -1;
            float bestDistance = maxDistance;
            for (int i = 0; i < Blocks.Count; i++)
            {
                float distance = Vector3.Distance(Center(Blocks[i]), localPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best < 0) return false;
            BlockRecord block = Blocks[best];
            if (block.paid)
            {
                Cost(block.Kind, out int wood, out int salvage);
                WreckWood += wood;
                WreckSalvage += salvage;
            }
            Blocks.RemoveAt(best);
            return true;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(new ShipSaveFile
            {
                blocks = Blocks.ToArray(),
                wreckWood = WreckWood,
                wreckSalvage = WreckSalvage,
            });
        }

        public static ShipLayout FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            ShipSaveFile file = JsonUtility.FromJson<ShipSaveFile>(json);
            if (file == null || file.blocks == null || file.blocks.Length == 0) return null;
            var layout = new ShipLayout();
            layout.Blocks.AddRange(file.blocks);
            layout.WreckWood = file.wreckWood;
            layout.WreckSalvage = file.wreckSalvage;
            return layout;
        }

        public void SaveTo(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ToJson());
        }

        public static ShipLayout LoadFrom(string path)
        {
            if (!File.Exists(path)) return null;
            try { return FromJson(File.ReadAllText(path)); }
            catch (Exception) { return null; }
        }

        int Find(int x, int y, int z)
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                BlockRecord block = Blocks[i];
                if (block.x == x && block.y == y && block.z == z) return i;
            }
            return -1;
        }

        int HighestAt(int x, int z)
        {
            int best = -1;
            int bestY = int.MinValue;
            for (int i = 0; i < Blocks.Count; i++)
            {
                BlockRecord block = Blocks[i];
                if (block.x == x && block.z == z && block.y >= bestY)
                {
                    bestY = block.y;
                    best = i;
                }
            }
            return best;
        }

        public int HullCount()
        {
            int count = 0;
            for (int i = 0; i < Blocks.Count; i++)
                if (Blocks[i].Kind == BlockKind.Hull) count++;
            return count;
        }
    }
}
