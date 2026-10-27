using NUnit.Framework;
using UnityEngine;

namespace MontAR.Tests
{
    public class OverlayMotionTests
    {
        private static readonly Vector3 Travel = new Vector3(0f, 0.02f, 0f);

        [Test]
        public void InsertLoop_StartsAtTravel_AndEndsSeated()
        {
            Vector3 start = OverlayMotionMath.Offset(OverlayMotionMode.InsertLoop, Travel, 2f, 0.25f, 0f);
            Vector3 seated = OverlayMotionMath.Offset(OverlayMotionMode.InsertLoop, Travel, 2f, 0.25f, 1.6f);

            Assert.AreEqual(0.02f, start.y, 1e-6f);
            Assert.AreEqual(Vector3.zero, seated);
        }

        [Test]
        public void InsertLoop_DescendsMonotonically_AndRepeats()
        {
            float previous = float.MaxValue;
            for (int i = 0; i <= 15; i++)
            {
                float y = OverlayMotionMath.Offset(OverlayMotionMode.InsertLoop, Travel, 2f, 0.25f, i * 0.1f).y;
                Assert.LessOrEqual(y, previous + 1e-6f);
                previous = y;
            }

            Vector3 nextCycle = OverlayMotionMath.Offset(OverlayMotionMode.InsertLoop, Travel, 2f, 0.25f, 2f);
            Assert.AreEqual(0.02f, nextCycle.y, 1e-6f);
        }

        [Test]
        public void Bob_StaysBetweenRestAndTravel()
        {
            for (int i = 0; i < 40; i++)
            {
                float y = OverlayMotionMath.Offset(OverlayMotionMode.Bob, Travel, 1.6f, 0f, i * 0.07f).y;
                Assert.GreaterOrEqual(y, -1e-6f);
                Assert.LessOrEqual(y, 0.02f + 1e-6f);
            }

            Assert.AreEqual(0f, OverlayMotionMath.Offset(OverlayMotionMode.Bob, Travel, 1.6f, 0f, 0f).y, 1e-6f);
            Assert.AreEqual(0.02f, OverlayMotionMath.Offset(OverlayMotionMode.Bob, Travel, 1.6f, 0f, 0.8f).y, 1e-6f);
        }

        [Test]
        public void InvalidPeriod_DoesNotMove()
        {
            Assert.AreEqual(Vector3.zero, OverlayMotionMath.Offset(OverlayMotionMode.Bob, Travel, 0f, 0f, 3f));
        }

        [Test]
        public void RoleFromColor_FollowsTheSpecColorConvention()
        {
            // Cores usadas no spec_montagem.json.
            Assert.AreEqual(OverlayPartRole.Piece, OverlayPart.RoleFromColor(new Color(0.1f, 0.85f, 0.4f, 0.35f)));
            Assert.AreEqual(OverlayPartRole.Target, OverlayPart.RoleFromColor(new Color(1f, 0.9f, 0.1f)));
            Assert.AreEqual(OverlayPartRole.Target, OverlayPart.RoleFromColor(new Color(1f, 0.8f, 0.1f)));
            Assert.AreEqual(OverlayPartRole.Lock, OverlayPart.RoleFromColor(new Color(0.95f, 0.15f, 0.15f)));
            Assert.AreEqual(OverlayPartRole.Gesture, OverlayPart.RoleFromColor(new Color(1f, 0.55f, 0.05f)));
            Assert.AreEqual(OverlayPartRole.Grip, OverlayPart.RoleFromColor(new Color(0.15f, 0.55f, 1f, 0.9f)));
            Assert.AreEqual(OverlayPartRole.Other, OverlayPart.RoleFromColor(new Color(0.5f, 0.5f, 0.5f)));
        }
    }
}
