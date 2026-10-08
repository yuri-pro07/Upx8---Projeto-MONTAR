using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class PartSelectionTests
    {
        private class FakeStep : IPartRequirement
        {
            public FakeStep(string id, string part = "", int min = 1, int max = 0, string model = "")
            {
                Id = id;
                RequiredPartId = part;
                MinPartQuantity = min;
                MaxPartQuantity = max;
                RequiredModelId = model;
            }

            public string Id { get; }
            public string RequiredPartId { get; }
            public string RequiredModelId { get; }
            public int MinPartQuantity { get; }
            public int MaxPartQuantity { get; }
        }

        private static List<PartDefinition> Catalog() => new List<PartDefinition>
        {
            new PartDefinition("cpu", "Processador", 1, 1, 1, new PartModel("lga1700", "LGA1700")),
            new PartDefinition("ram", "Memória", 1, 2, 2, new PartModel("ddr5", "DDR5"), new PartModel("ddr4", "DDR4")),
            new PartDefinition("gpu", "Placa de vídeo", 0, 1, 1, new PartModel("pcie", "PCIe x16")),
        };

        [Test]
        public void Default_UsesFirstModelAndDefaultQuantity()
        {
            PartSelection kit = PartSelection.CreateDefault(Catalog());

            Assert.AreEqual(3, kit.Count);
            Assert.AreEqual("ddr5", kit.GetModelId("ram"));
            Assert.AreEqual(2, kit.GetQuantity("ram"));
            Assert.IsTrue(kit.Includes("gpu"));
            Assert.AreEqual("cpu:lga1700:1|ram:ddr5:2|gpu:pcie:1", kit.Describe());
        }

        [Test]
        public void Set_ReplacesEntry_AndKeepsOrder()
        {
            PartSelection kit = PartSelection.CreateDefault(Catalog());

            kit.Set("ram", "ddr4", 1);
            kit.Set("gpu", "pcie", 0);

            Assert.AreEqual("cpu:lga1700:1|ram:ddr4:1|gpu:pcie:0", kit.Describe());
            Assert.IsFalse(kit.Includes("gpu"));
            Assert.AreEqual(0, kit.GetQuantity("ssd"));
            Assert.AreEqual(string.Empty, kit.GetModelId("ssd"));
        }

        [Test]
        public void Set_RejectsInvalidValues()
        {
            var kit = new PartSelection();
            Assert.Throws<System.ArgumentException>(() => kit.Set("", "x", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => kit.Set("ram", "x", -1));
        }

        [Test]
        public void Entries_RoundTrip_AndAreCopies()
        {
            PartSelection kit = PartSelection.CreateDefault(Catalog());
            List<PartSelectionEntry> saved = kit.ToEntries();
            saved[1].quantity = 1;

            Assert.AreEqual(2, kit.GetQuantity("ram"), "ToEntries devolve cópias");
            Assert.AreEqual("cpu:lga1700:1|ram:ddr5:1|gpu:pcie:1", PartSelection.FromEntries(saved).Describe());
        }

        [Test]
        public void FromEntries_IgnoresBrokenLines()
        {
            var saved = new List<PartSelectionEntry>
            {
                null,
                new PartSelectionEntry { partId = "", modelId = "x", quantity = 1 },
                new PartSelectionEntry { partId = "ram", modelId = "ddr5", quantity = -3 },
                new PartSelectionEntry { partId = "cpu", modelId = "lga1700", quantity = 1 },
            };

            Assert.AreEqual("cpu:lga1700:1", PartSelection.FromEntries(saved).Describe());
            Assert.AreEqual(0, PartSelection.FromEntries(null).Count);
        }

        [Test]
        public void PartDefinition_ClampsQuantity()
        {
            PartDefinition ram = Catalog()[1];
            Assert.AreEqual(1, ram.ClampQuantity(0));
            Assert.AreEqual(2, ram.ClampQuantity(5));
            Assert.IsTrue(ram.IsRequired);
            Assert.IsFalse(Catalog()[2].IsRequired);
            Assert.AreEqual("ddr4", ram.FindModel("ddr4").ModelId);
            Assert.IsNull(ram.FindModel("ddr3"));
        }

        [Test]
        public void StepFilter_FollowsKit()
        {
            var steps = new[]
            {
                new FakeStep("placa"),
                new FakeStep("cpu", "cpu"),
                new FakeStep("ram_a2", "ram", 1, 1),
                new FakeStep("ram_a2_b2", "ram", 2, 2),
                new FakeStep("gpu", "gpu"),
                new FakeStep("final"),
            };
            PartSelection kit = PartSelection.CreateDefault(Catalog());

            CollectionAssert.AreEqual(new[] { "placa", "cpu", "ram_a2_b2", "gpu", "final" },
                StepFilter.Filter(steps, kit).Select(s => s.Id).ToArray());

            kit.Set("ram", "ddr5", 1);
            kit.Set("gpu", "pcie", 0);
            CollectionAssert.AreEqual(new[] { "placa", "cpu", "ram_a2", "final" },
                StepFilter.Filter(steps, kit).Select(s => s.Id).ToArray());
        }

        [Test]
        public void StepFilter_ChecksModel_AndTreatsMinBelowOneAsOne()
        {
            PartSelection kit = PartSelection.CreateDefault(Catalog());

            Assert.IsTrue(StepFilter.Applies(new FakeStep("ddr5", "ram", model: "ddr5"), kit));
            Assert.IsFalse(StepFilter.Applies(new FakeStep("ddr4", "ram", model: "ddr4"), kit));
            Assert.IsFalse(StepFilter.Applies(new FakeStep("ssd", "ssd", 0), kit), "peça fora do kit não entra nem com mínimo 0");
            Assert.IsTrue(StepFilter.Applies(new FakeStep("sempre"), null));
            Assert.IsFalse(StepFilter.Applies(new FakeStep("cpu", "cpu"), null));
            Assert.IsFalse(StepFilter.Applies(null, kit));
        }
    }
}
