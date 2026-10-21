using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Sailing.Tests
{
    public class SliceLogicTests
    {
        [Test]
        public void GerstnerSampleIsStableAndBounded()
        {
            var xz = new Vector2(2f, 4f);
            GerstnerField.Sample(xz, 3.5f, 4, out Vector3 a, out Vector3 normal);
            GerstnerField.Sample(xz, 3.5f, 4, out Vector3 b, out _);
            Assert.AreEqual(a, b);
            Assert.LessOrEqual(Mathf.Abs(a.y), 0.32f + 0.16f + 0.08f + 0.04f + 0.001f);
            Assert.Greater(normal.y, 0.5f);
            Assert.AreEqual(1f, normal.magnitude, 0.001f);
            GerstnerField.Sample(xz, 3.5f, 3, out Vector3 mobile, out _);
            Assert.IsFalse(float.IsNaN(mobile.y));
        }

        [Test]
        public void WindBlowsEast()
        {
            Vector3 direction = Wind.Direction(0f);
            Assert.Greater(direction.x, 0.8f);
            Assert.AreEqual(1f, direction.magnitude, 0.001f);
        }

        [Test]
        public void BroadsideArcIsAbeamOnly()
        {
            Assert.IsTrue(BroadsideMath.InArc(Vector3.zero, Vector3.right, new Vector3(10f, 0f, 0f), BroadsideMath.Range, BroadsideMath.MinAbeamDot));
            Assert.IsTrue(BroadsideMath.InArc(Vector3.zero, Vector3.right, new Vector3(-8f, 1f, 2f), BroadsideMath.Range, BroadsideMath.MinAbeamDot));
            Assert.IsFalse(BroadsideMath.InArc(Vector3.zero, Vector3.right, new Vector3(0f, 0f, 10f), BroadsideMath.Range, BroadsideMath.MinAbeamDot));
            Assert.IsFalse(BroadsideMath.InArc(Vector3.zero, Vector3.right, new Vector3(30f, 0f, 0f), BroadsideMath.Range, BroadsideMath.MinAbeamDot));
        }

        [Test]
        public void HelmPrefersTheStrongerStick()
        {
            Assert.AreEqual(new Vector2(0f, 1f), SailInput.CombineHelm(new Vector2(0.2f, 0f), new Vector2(0f, 1f)));
            Assert.AreEqual(new Vector2(-1f, 0f), SailInput.CombineHelm(new Vector2(-1f, 0f), Vector2.zero));
        }

        [Test]
        public void ShipGridPlacesRefundsAndRefusesABareRaft()
        {
            ShipLayout layout = ShipLayout.Starter();
            int wood = 0;
            int salvage = 0;
            Assert.IsFalse(layout.TryPlace(2, 0, 0, BlockKind.Hull, ref wood, ref salvage, out _));
            wood = 4;
            Assert.IsTrue(layout.TryPlace(2, 0, 0, BlockKind.Hull, ref wood, ref salvage, out _));
            Assert.AreEqual(0, wood);
            Assert.IsTrue(layout.TryRemove(2, 0, 0, ref wood, ref salvage, out _));
            Assert.AreEqual(4, wood);

            wood = 10;
            salvage = 5;
            Assert.IsFalse(layout.TryPlace(2, 0, 3, BlockKind.Hull, ref wood, ref salvage, out _));
            Assert.IsFalse(layout.TryPlace(0, 0, 9, BlockKind.Hull, ref wood, ref salvage, out _));
            Assert.AreEqual(0f, layout.MastBonus);
            Assert.IsTrue(layout.TryPlace(0, 1, 0, BlockKind.Mast, ref wood, ref salvage, out _));
            Assert.Greater(layout.MastBonus, 0f);
            Assert.IsFalse(layout.HasCannon);
            Assert.IsTrue(layout.TryPlace(1, 1, 0, BlockKind.Cannon, ref wood, ref salvage, out _));
            Assert.IsTrue(layout.HasCannon);

            int y;
            Assert.IsTrue(layout.TryResolve(2, 0, 1, out y));
            Assert.AreEqual(0, y);

            while (layout.HullCount() > 1)
            {
                BlockRecord hull = null;
                for (int i = 0; i < layout.Blocks.Count; i++)
                    if (layout.Blocks[i].Kind == BlockKind.Hull) hull = layout.Blocks[i];
                Assert.IsTrue(layout.TryRemove(hull.x, hull.y, hull.z, ref wood, ref salvage, out _));
            }
            BlockRecord last = null;
            for (int i = 0; i < layout.Blocks.Count; i++)
                if (layout.Blocks[i].Kind == BlockKind.Hull) last = layout.Blocks[i];
            Assert.IsFalse(layout.TryRemove(last.x, last.y, last.z, ref wood, ref salvage, out _));
            Assert.IsTrue(layout.HasHull);
        }

        [Test]
        public void CombatBanksPaidBlocksAndSaveRoundTrips()
        {
            ShipLayout layout = ShipLayout.Starter();
            int wood = 8;
            int salvage = 0;
            Assert.IsTrue(layout.TryPlace(2, 0, 0, BlockKind.Hull, ref wood, ref salvage, out _));
            Assert.IsTrue(layout.DamageAt(ShipLayout.Center(new BlockRecord { x = 2, y = 0, z = 0, kind = (int)BlockKind.Hull }), 1.5f));
            Assert.AreEqual(4, layout.WreckWood);
            Assert.IsTrue(layout.HasHull);

            string path = Path.Combine(Application.temporaryCachePath, "ship-test.json");
            layout.SaveTo(path);
            ShipLayout loaded = ShipLayout.LoadFrom(path);
            Assert.AreEqual(layout.Blocks.Count, loaded.Blocks.Count);
            Assert.AreEqual(4, loaded.WreckWood);
            File.Delete(path);
        }
    }
}
