using System.Collections.Generic;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class ValidationTests
    {
        [Test]
        public void Checklist_WithoutItems_IsSatisfied()
        {
            var checklist = new ChecklistValidator(new List<string>());
            Assert.IsTrue(checklist.IsSatisfied);
            Assert.AreEqual("checked=0/0", checklist.Describe());

            Assert.IsTrue(new ChecklistValidator(null).IsSatisfied);
        }

        [Test]
        public void Checklist_RequiresAllItems()
        {
            var checklist = new ChecklistValidator(new[] { "Alavanca travada", "Triângulo alinhado", "Sem forçar" });
            checklist.SetChecked(0, true);
            checklist.SetChecked(2, true);

            Assert.IsFalse(checklist.IsSatisfied);
            Assert.AreEqual("checked=2/3;pending=1", checklist.Describe());

            checklist.SetChecked(1, true);
            Assert.IsTrue(checklist.IsSatisfied);
            Assert.AreEqual("checked=3/3", checklist.Describe());
        }

        [Test]
        public void ReferenceSeen_FollowsTrackingState()
        {
            bool tracked = false;
            var validator = new ReferenceSeenValidator("marcador_ram", name => name == "marcador_ram" && tracked);

            Assert.IsFalse(validator.IsSatisfied);
            tracked = true;
            Assert.IsTrue(validator.IsSatisfied);
            Assert.AreEqual("seen=marcador_ram", validator.Describe());
        }
    }
}
