using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MontAR.Tests
{
    public class StudyTests
    {
        private static StudyCameraInfo Camera() =>
            new StudyCameraInfo(new Vector3(0.2f, 1.1f, -0.4f), Quaternion.Euler(25f, 40f, 5f), 0.6f, 0.46f);

        private static StudyContent LoadContent() =>
            StudyContent.Parse(File.ReadAllText("Assets/Data/Estudo/estudo_pc.json"));

        [Test]
        public void Content_IsComplete()
        {
            CollectionAssert.IsEmpty(LoadContent().Validate());
        }

        [Test]
        public void Content_CoversEveryPartOfTheModel_InAssemblyOrder()
        {
            StudyContent content = LoadContent();

            CollectionAssert.AreEquivalent(StudyPcModel.PartIds, content.parts.Select(p => p.id));
            CollectionAssert.AreEqual(StudyPcModel.PartIds, content.parts.Select(p => p.id), "a lista segue a ordem da montagem");
        }

        [Test]
        public void Validate_ReportsMissingFieldsAndRepeats()
        {
            var content = new StudyContent();
            content.parts.Add(new StudyPartInfo { id = "a", sequence = 1 });
            content.parts.Add(new StudyPartInfo { id = "a", sequence = 1 });

            var problems = content.Validate();

            Assert.IsTrue(problems.Any(p => p.Contains("id repetido")));
            Assert.IsTrue(problems.Any(p => p.Contains("sequence repetida")));
            Assert.IsTrue(problems.Any(p => p.Contains("falta whatItDoes")));
            Assert.IsTrue(problems.Any(p => p.Contains("falta details")));
        }

        [Test]
        public void Focus_PutsThePartOnTheFreeAreaCenter_FacingTheCamera()
        {
            StudyCameraInfo camera = Camera();
            var center = new Vector3(-0.05f, 0.3f, 0.1f);
            Vector3 view = new Vector3(1f, 0.2f, 0.3f).normalized;

            StudyPose pose = StudyFraming.Focus(camera, 0.4f, 0.9f, 0.42f, center, view, 0.03f, 1.2f, 0f, 0f, 1f);

            Vector3 world = pose.Position + pose.Rotation * (center * pose.Scale);
            Vector3 expected = StudyFraming.ViewPoint(camera, 0.65f, 0.42f);
            Assert.That(Vector3.Distance(expected, world), Is.LessThan(1e-4f));
            Assert.That(Vector3.Dot(pose.Rotation * view, -camera.Forward), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void Focus_UserZoomMultipliesTheScale_AndKeepsThePartCentered()
        {
            StudyCameraInfo camera = Camera();
            var center = new Vector3(0.02f, 0.2f, -0.1f);

            StudyPose normal = StudyFraming.Focus(camera, 0.3f, 0.8f, 0.42f, center, Vector3.right, 0.1f, 1.2f, 0f, 0f, 1f);
            StudyPose zoomed = StudyFraming.Focus(camera, 0.3f, 0.8f, 0.42f, center, Vector3.right, 0.1f, 1.2f, 0f, 0f, 2f);

            Assert.That(zoomed.Scale, Is.EqualTo(normal.Scale * 2f).Within(1e-4f));
            Vector3 world = zoomed.Position + zoomed.Rotation * (center * zoomed.Scale);
            Assert.That(Vector3.Distance(StudyFraming.ViewPoint(camera, 0.55f, 0.42f), world), Is.LessThan(1e-4f));
        }

        [Test]
        public void FitScale_InPortrait_IsLimitedByTheWidth()
        {
            var narrow = new StudyCameraInfo(Vector3.zero, Quaternion.identity, 0.6f, 0.4f);

            float scale = StudyFraming.FitScale(narrow, 0f, 1f, 0.5f, 0.1f, 1f);

            Assert.That(scale, Is.EqualTo(0.5f * 0.6f * 0.4f / 0.1f).Within(1e-4f));
        }

        [Test]
        public void FacingYaw_TurnsTheOpenSideToTheCamera()
        {
            var anchor = new Vector3(1f, 0f, 2f);
            var viewer = new Vector3(1f, 0.5f, 3f);

            float yaw = StudyFraming.FacingYaw(anchor, viewer, 0f);

            Vector3 openSide = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Assert.That(Vector3.Dot(openSide, Vector3.forward), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void StageCamera_KeepsTheFocusOnTheFreeAreaCenter()
        {
            Quaternion rotation = Quaternion.Euler(15f, 180f, 0f);
            var focus = new Vector3(0f, -199.8f, 0f);

            Vector3 position = StudyFraming.StageCameraPosition(rotation, 0.47f, 0.46f, focus, 0.36f, 1f, 0.25f, 0.85f);

            float distance = Vector3.Dot(focus - position, rotation * Vector3.forward);
            var camera = new StudyCameraInfo(position, rotation, 0.47f, 0.46f);
            Assert.That(Vector3.Distance(focus, StudyFraming.ViewPoint(camera, 0.55f, distance)), Is.LessThan(1e-3f));
        }
    }
}
