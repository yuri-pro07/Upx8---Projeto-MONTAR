using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MontAR.Tests
{
    public class ProgressStoreTests
    {
        private string directory;
        private ProgressStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "MontAR-tests-" + Guid.NewGuid().ToString("N"));
            store = new ProgressStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void Load_WithoutFile_ReturnsFalse()
        {
            Assert.IsFalse(store.TryLoad(out SessionProgress progress));
            Assert.IsNull(progress);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            store.Save(new SessionProgress
            {
                sessionId = "P07_20261020T143000",
                participantCode = "P07",
                procedureId = "montagem_v0",
                completedSteps = 4
            });

            Assert.IsTrue(store.TryLoad(out SessionProgress loaded));
            Assert.AreEqual("P07_20261020T143000", loaded.sessionId);
            Assert.AreEqual("P07", loaded.participantCode);
            Assert.AreEqual("montagem_v0", loaded.procedureId);
            Assert.AreEqual(4, loaded.completedSteps);
            Assert.IsFalse(loaded.finished);
            Assert.IsFalse(string.IsNullOrEmpty(loaded.updatedUtc));
        }

        [Test]
        public void CorruptedFile_ReturnsFalse()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.FilePath, "{ isto não é json");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Progresso salvo ilegível"));
            Assert.IsFalse(store.TryLoad(out SessionProgress progress));
            Assert.IsNull(progress);
        }

        [Test]
        public void Clear_RemovesSavedProgress()
        {
            store.Save(new SessionProgress { sessionId = "P01_x" });
            store.Clear();
            Assert.IsFalse(File.Exists(store.FilePath));
            Assert.IsFalse(store.TryLoad(out _));
        }
    }
}
