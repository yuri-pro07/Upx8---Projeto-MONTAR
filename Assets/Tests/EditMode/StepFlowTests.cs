using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class StepFlowTests
    {
        private class FakeStep : IStepDefinition
        {
            public FakeStep(string id, string reference)
            {
                StepId = id;
                ReferenceImageName = reference;
            }

            public string StepId { get; }
            public string ReferenceImageName { get; }
        }

        private List<FlowEvent> events;

        private StepFlow CreateFlow(params IStepDefinition[] steps)
        {
            var flow = new StepFlow(steps);
            events = new List<FlowEvent>();
            flow.EventRaised += events.Add;
            return flow;
        }

        private static StepFlow ValidateCurrent(StepFlow flow, bool passed = true)
        {
            Assert.IsTrue(flow.BeginValidation());
            Assert.IsTrue(flow.SubmitValidation(passed));
            return flow;
        }

        private List<FlowEventType> Types() => events.Select(e => e.Type).ToList();

        [Test]
        public void Constructor_RejectsEmptyOrNullSteps()
        {
            Assert.Throws<ArgumentException>(() => new StepFlow(new IStepDefinition[0]));
            Assert.Throws<ArgumentException>(() => new StepFlow(null));
            Assert.Throws<ArgumentException>(() => new StepFlow(new IStepDefinition[] { new FakeStep("a", ""), null }));
        }

        [Test]
        public void Start_EmitsSessionAndStepStarted_AndWaitsForReference()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));

            flow.Start();

            CollectionAssert.AreEqual(new[] { FlowEventType.SessionStarted, FlowEventType.StepStarted }, Types());
            Assert.AreEqual(-1, events[0].StepIndex);
            Assert.AreEqual(0, events[1].StepIndex);
            Assert.AreEqual("cpu", events[1].StepId);
            Assert.AreEqual(StepPhase.Locating, flow.Phase);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            StepFlow flow = CreateFlow(new FakeStep("a", ""));
            flow.Start();
            Assert.Throws<InvalidOperationException>(() => flow.Start());
        }

        [Test]
        public void StepWithoutReference_StartsInGuiding()
        {
            StepFlow flow = CreateFlow(new FakeStep("espuma", ""));
            flow.Start();
            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
        }

        [Test]
        public void Validation_IsBlocked_WhileLocating()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            Assert.IsFalse(flow.BeginValidation());
            Assert.IsFalse(flow.SubmitValidation(true));
            Assert.AreEqual(StepPhase.Locating, flow.Phase);
            Assert.AreEqual(0, flow.CurrentIndex);
            Assert.IsEmpty(events);
        }

        [Test]
        public void ReferenceOfCurrentStep_MovesToGuiding()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            flow.NotifyReferenceDetected("tapete");

            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            Assert.AreEqual(FlowEventType.ReferenceDetected, events.Single().Type);
            Assert.AreEqual("tapete", events.Single().Detail);
        }

        [Test]
        public void OtherReference_IsLogged_ButKeepsLocating()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            flow.NotifyReferenceDetected("marcador_ram");

            Assert.AreEqual(StepPhase.Locating, flow.Phase);
            Assert.AreEqual("marcador_ram", events.Single().Detail);
        }

        [Test]
        public void RepeatedDetection_IsIgnoredUntilLost()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            flow.NotifyReferenceDetected("tapete");
            flow.NotifyReferenceDetected("tapete");
            flow.NotifyReferenceLost("tapete");
            flow.NotifyReferenceLost("tapete");
            flow.NotifyReferenceDetected("tapete");

            CollectionAssert.AreEqual(
                new[] { FlowEventType.ReferenceDetected, FlowEventType.TrackingLost, FlowEventType.ReferenceDetected },
                Types());
        }

        [Test]
        public void ReferenceAlreadyTracked_IsDetectedAtStepStart()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.NotifyReferenceDetected("tapete");

            flow.Start();

            CollectionAssert.AreEqual(
                new[] { FlowEventType.SessionStarted, FlowEventType.StepStarted, FlowEventType.ReferenceDetected },
                Types());
            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
        }

        [Test]
        public void TrackingLost_DoesNotRegressPhase()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            flow.NotifyReferenceDetected("tapete");

            flow.NotifyReferenceLost("tapete");

            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            Assert.IsTrue(flow.BeginValidation());
        }

        [Test]
        public void FailedValidation_ReturnsToGuiding_WithoutAdvancing()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""), new FakeStep("ram", ""));
            flow.Start();
            flow.BeginValidation();
            events.Clear();

            flow.SubmitValidation(false, "checked=1/2;pending=1");

            Assert.AreEqual(0, flow.CurrentIndex);
            Assert.AreEqual(0, flow.CompletedCount);
            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            Assert.AreEqual(FlowEventType.ValidationFailed, events.Single().Type);
            Assert.AreEqual("checked=1/2;pending=1", events.Single().Detail);
        }

        [Test]
        public void CancelledValidation_ReturnsToGuiding()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""));
            flow.Start();
            flow.BeginValidation();

            Assert.IsTrue(flow.CancelValidation());

            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            Assert.AreEqual(FlowEventType.ValidationCancelled, events.Last().Type);
            Assert.IsFalse(flow.CancelValidation());
        }

        [Test]
        public void PassedValidation_CompletesStep_AndStartsNext()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""), new FakeStep("ram", "tapete"));
            flow.Start();
            flow.BeginValidation();
            events.Clear();

            flow.SubmitValidation(true, "checked=2/2");

            CollectionAssert.AreEqual(
                new[] { FlowEventType.ValidationPassed, FlowEventType.StepCompleted, FlowEventType.StepStarted },
                Types());
            Assert.AreEqual(0, events[1].StepIndex);
            Assert.AreEqual(1, events[2].StepIndex);
            Assert.AreEqual("ram", events[2].StepId);
            Assert.AreEqual(1, flow.CurrentIndex);
            Assert.AreEqual(1, flow.CompletedCount);
            Assert.AreEqual(StepPhase.Locating, flow.Phase);
        }

        [Test]
        public void LastStep_CompletesSession()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""), new FakeStep("ram", ""));
            flow.Start();
            ValidateCurrent(flow);
            ValidateCurrent(flow);

            Assert.IsTrue(flow.IsFinished);
            Assert.IsFalse(flow.IsRunning);
            Assert.IsNull(flow.CurrentStep);
            Assert.AreEqual(2, flow.CompletedCount);
            Assert.AreEqual(FlowEventType.SessionCompleted, events.Last().Type);
            Assert.AreEqual(-1, events.Last().StepIndex);

            Assert.IsFalse(flow.BeginValidation());
            Assert.IsFalse(flow.RegisterObserverIntervention());
        }

        [Test]
        public void Resume_StartsAtSavedStep()
        {
            StepFlow flow = CreateFlow(new FakeStep("a", ""), new FakeStep("b", ""), new FakeStep("c", ""));

            flow.Start(2);

            Assert.AreEqual("resumed_from=2", events[0].Detail);
            Assert.AreEqual(2, flow.CurrentIndex);
            Assert.AreEqual(2, flow.CompletedCount);
            Assert.AreEqual("c", events[1].StepId);
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateFlow(new FakeStep("a", "")).Start(1));
        }

        [Test]
        public void ObserverIntervention_IsRecordedOnCurrentStep()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""), new FakeStep("ram", ""));
            Assert.IsFalse(flow.RegisterObserverIntervention());

            flow.Start();
            ValidateCurrent(flow);
            Assert.IsTrue(flow.RegisterObserverIntervention("dica_verbal"));

            FlowEvent last = events.Last();
            Assert.AreEqual(FlowEventType.ObserverIntervention, last.Type);
            Assert.AreEqual(1, last.StepIndex);
            Assert.AreEqual("ram", last.StepId);
            Assert.AreEqual("dica_verbal", last.Detail);
        }

        [Test]
        public void FullSession_ProducesExpectedEventSequence()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"), new FakeStep("ram", "tapete"));
            flow.Start();
            flow.NotifyReferenceDetected("tapete");
            flow.BeginValidation();
            flow.SubmitValidation(false);
            flow.BeginValidation();
            flow.SubmitValidation(true);
            flow.BeginValidation();
            flow.SubmitValidation(true);

            CollectionAssert.AreEqual(new[]
            {
                "session_started",
                "step_started", "reference_detected",
                "validation_started", "validation_failed",
                "validation_started", "validation_passed", "step_completed",
                "step_started", "reference_detected",
                "validation_started", "validation_passed", "step_completed",
                "session_completed"
            }, events.Select(e => e.Type.ToCsvName()).ToArray());
        }
    }
}
