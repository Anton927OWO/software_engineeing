using NUnit.Framework;
using UnityEngine;

namespace TrackingNIU.XR.Tests
{
    public class TableAnchorTests
    {
        [Test]
        public void VirtualPose_IsInFrontOfHead_AndFlat()
        {
            var pose = TableAnchor.ComputeVirtualPose(new Vector3(1f, 1.6f, 2f), new Vector3(0f, -0.5f, 1f), 0.5f, 0.6f, 0.7f, 1.0f);
            Assert.AreEqual(1f, pose.position.x, 1e-4f);
            Assert.AreEqual(2.5f, pose.position.z, 1e-4f);
            Assert.AreEqual(1.0f, pose.position.y, 1e-4f);
            Assert.AreEqual(0f, (pose.rotation * Vector3.forward).y, 1e-4f);
        }

        [Test]
        public void VirtualPose_HeightIsClamped()
        {
            var seated = TableAnchor.ComputeVirtualPose(new Vector3(0f, 1.1f, 0f), Vector3.forward, 0.5f, 0.6f, 0.7f, 1.0f);
            Assert.AreEqual(0.7f, seated.position.y, 1e-4f);
            var tall = TableAnchor.ComputeVirtualPose(new Vector3(0f, 1.9f, 0f), Vector3.forward, 0.5f, 0.6f, 0.7f, 1.0f);
            Assert.AreEqual(1.0f, tall.position.y, 1e-4f);
        }

        [Test]
        public void VirtualPose_LookingStraightDown_StillValid()
        {
            var pose = TableAnchor.ComputeVirtualPose(Vector3.up * 1.6f, Vector3.down, 0.5f, 0.6f, 0.7f, 1.0f);
            Assert.AreEqual(0.5f, pose.position.z, 1e-4f);
        }

        [Test]
        public void FacingAwayFrom_PointsFromHeadToTable()
        {
            var rotation = TableAnchor.FacingAwayFrom(new Vector3(0f, 1.6f, 0f), new Vector3(1f, 0.8f, 0f));
            var forward = rotation * Vector3.forward;
            Assert.AreEqual(1f, forward.x, 1e-4f);
            Assert.AreEqual(0f, forward.y, 1e-4f);
        }

        [Test]
        public void HapticPresets_AreInRange()
        {
            foreach (HapticPreset preset in System.Enum.GetValues(typeof(HapticPreset)))
            {
                Haptics.GetPreset(preset, out var amplitude, out var duration);
                Assert.That(amplitude, Is.InRange(0f, 1f));
                Assert.That(duration, Is.GreaterThan(0f));
            }
        }
    }
}
