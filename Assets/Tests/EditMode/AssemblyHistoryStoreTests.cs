using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MontAR.Tests
{
    public class AssemblyHistoryStoreTests
    {
        private string directory;
        private AssemblyHistoryStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "MontAR-history-" + Guid.NewGuid().ToString("N"));
            store = new AssemblyHistoryStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private static HistoryEntry Entry(string sessionId, string label = "") =>
            new HistoryEntry { sessionId = sessionId, participantCode = "P01", label = label, elapsedSeconds = 60 };

        [Test]
        public void Load_WithoutFile_IsEmpty()
        {
            Assert.IsEmpty(store.Load());
        }

        [Test]
        public void Add_KeepsNewestFirst_AndRoundTrips()
        {
            store.Add(Entry("A", "Bancada 1"));
            store.Add(Entry("B", "Bancada 2"));

            var entries = store.Load();
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("B", entries[0].sessionId);
            Assert.AreEqual("Bancada 2", entries[0].label);
            Assert.AreEqual("A", entries[1].sessionId);
            Assert.IsNotEmpty(entries[0].finishedUtc, "data do fim preenchida");
        }

        [Test]
        public void Add_SameSession_ReplacesPreviousEntry()
        {
            store.Add(Entry("A"));
            store.Add(Entry("B"));
            HistoryEntry again = Entry("A", "retomada");
            store.Add(again);

            var entries = store.Load();
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("A", entries[0].sessionId);
            Assert.AreEqual("retomada", entries[0].label);
        }

        [Test]
        public void Add_DropsOldestBeyondLimit()
        {
            for (int i = 0; i < AssemblyHistoryStore.MaxEntries + 5; i++)
                store.Add(Entry("S" + i));

            var entries = store.Load();
            Assert.AreEqual(AssemblyHistoryStore.MaxEntries, entries.Count);
            Assert.AreEqual("S" + (AssemblyHistoryStore.MaxEntries + 4), entries[0].sessionId);
        }

        [Test]
        public void Add_WithoutSessionId_Throws()
        {
            Assert.Throws<ArgumentException>(() => store.Add(new HistoryEntry()));
        }

        [Test]
        public void Load_CorruptFile_IsEmpty()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.FilePath, "{ isto não é json");
            LogAssert.ignoreFailingMessages = true;
            Assert.IsEmpty(store.Load());
        }

        [TestCase(null, "")]
        [TestCase("   ", "")]
        [TestCase("  Bancada   3 \t HP ", "Bancada 3 HP")]
        public void NormalizeLabel_TrimsAndCollapsesSpaces(string raw, string expected)
        {
            Assert.AreEqual(expected, AssemblyHistoryStore.NormalizeLabel(raw));
        }

        [Test]
        public void NormalizeLabel_CutsAtMaxLength()
        {
            string label = AssemblyHistoryStore.NormalizeLabel(new string('x', 80));
            Assert.AreEqual(AssemblyHistoryStore.MaxLabelLength, label.Length);
        }

        [Test]
        public void CreateEntry_CopiesSummary()
        {
            double now = 0;
            var summary = new SessionSummary(() => now);
            summary.Record(new FlowEvent(FlowEventType.SessionStarted, -1, "", ""));
            summary.Record(new FlowEvent(FlowEventType.StepStarted, 0, "cpu", ""));
            summary.Record(new FlowEvent(FlowEventType.ValidationStarted, 0, "cpu", ""));
            summary.Record(new FlowEvent(FlowEventType.ValidationFailed, 0, "cpu", ""));
            summary.Record(new FlowEvent(FlowEventType.ObserverIntervention, 0, "cpu", ""));
            now = 30;
            summary.Record(new FlowEvent(FlowEventType.StepCompleted, 0, "cpu", ""));
            summary.Record(new FlowEvent(FlowEventType.SessionCompleted, -1, "", ""));
            var progress = new SessionProgress { sessionId = "P01_X", participantCode = "P01", label = "  Bancada  3 " };

            HistoryEntry entry = AssemblyHistoryStore.CreateEntry(progress, summary, null, GuidanceMode.Demo3D, "cpu:amd_am5:1");

            Assert.AreEqual("P01_X", entry.sessionId);
            Assert.AreEqual("Bancada 3", entry.label);
            Assert.AreEqual("demo_3d", entry.mode);
            Assert.AreEqual("cpu:amd_am5:1", entry.kit);
            Assert.AreEqual(30, entry.elapsedSeconds, 1e-9);
            Assert.AreEqual(1, entry.completedSteps);
            Assert.AreEqual(1, entry.failedValidations);
            Assert.AreEqual(1, entry.observerInterventions);
            Assert.IsFalse(entry.resumed);
            Assert.AreEqual(1, entry.steps.Count);
            Assert.AreEqual("cpu", entry.steps[0].title, "sem a etapa, o título cai no id");
            Assert.AreEqual(1, entry.steps[0].failedValidations);
        }

        [Test]
        public void DisplayName_WithoutLabel()
        {
            Assert.AreEqual("Montagem sem nome", Entry("A").DisplayName);
            Assert.AreEqual("Bancada 1", Entry("A", "Bancada 1").DisplayName);
        }
    }
}
