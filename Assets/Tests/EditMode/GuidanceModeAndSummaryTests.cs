using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class GuidanceModeAndSummaryTests
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

        [Test]
        public void Start_AppendsSessionDetail()
        {
            StepFlow flow = CreateFlow(new FakeStep("a", ""), new FakeStep("b", ""));
            flow.Start(1, "mode=ar;kit=cpu:x:1");

            Assert.AreEqual("resumed_from=1;mode=ar;kit=cpu:x:1", events[0].Detail);

            StepFlow fresh = CreateFlow(new FakeStep("a", ""));
            fresh.Start(0, "mode=demo_3d");
            Assert.AreEqual("mode=demo_3d", events[0].Detail);
        }

        [Test]
        public void Demo3D_BeforeStart_SkipsLocating_WithoutFakeDetection()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.SetMode(GuidanceMode.Demo3D);

            flow.Start();

            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            CollectionAssert.AreEqual(new[] { FlowEventType.SessionStarted, FlowEventType.StepStarted }, events.Select(e => e.Type).ToArray());
            Assert.IsTrue(flow.BeginValidation());
        }

        [Test]
        public void SwitchingToDemo_WhileLocating_MovesToGuiding_AndIsLogged()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            flow.SetMode(GuidanceMode.Demo3D);

            Assert.AreEqual(StepPhase.Guiding, flow.Phase);
            Assert.AreEqual(FlowEventType.ModeChanged, events.Single().Type);
            Assert.AreEqual("mode=demo_3d", events.Single().Detail);
            Assert.AreEqual("mode_changed", events.Single().Type.ToCsvName());
        }

        [Test]
        public void BackToAr_DoesNotRegressPhase_AndNextStepWaitsForReference()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"), new FakeStep("ram", "tapete"));
            flow.Start();
            flow.SetMode(GuidanceMode.Demo3D);

            flow.SetMode(GuidanceMode.AugmentedReality);
            Assert.AreEqual(StepPhase.Guiding, flow.Phase);

            flow.BeginValidation();
            flow.SubmitValidation(true);
            Assert.AreEqual(1, flow.CurrentIndex);
            Assert.AreEqual(StepPhase.Locating, flow.Phase);
        }

        [Test]
        public void StepStarted_AlreadyCarriesThePhase()
        {
            // A interface atualiza no step_started: a fase precisa estar certa nesse momento.
            StepFlow flow = CreateFlow(new FakeStep("espuma", ""), new FakeStep("cpu", "tapete"), new FakeStep("ram", "tapete"));
            var phases = new List<StepPhase>();
            flow.EventRaised += e =>
            {
                if (e.Type == FlowEventType.StepStarted)
                    phases.Add(flow.Phase);
            };

            flow.Start();
            flow.BeginValidation();
            flow.SubmitValidation(true);
            flow.SetMode(GuidanceMode.Demo3D);
            flow.BeginValidation();
            flow.SubmitValidation(true);

            CollectionAssert.AreEqual(new[] { StepPhase.Guiding, StepPhase.Locating, StepPhase.Guiding }, phases);
        }

        [Test]
        public void SameMode_IsIgnored()
        {
            StepFlow flow = CreateFlow(new FakeStep("cpu", "tapete"));
            flow.Start();
            events.Clear();

            flow.SetMode(GuidanceMode.AugmentedReality);

            Assert.IsEmpty(events);
            Assert.AreEqual(StepPhase.Locating, flow.Phase);
        }

        [Test]
        public void Summary_CountsAttemptsTimesAndInterventions()
        {
            double now = 0;
            var summary = new SessionSummary(() => now);
            StepFlow flow = CreateFlow(new FakeStep("cpu", ""), new FakeStep("ram", ""));
            flow.EventRaised += summary.Record;

            flow.Start();
            now = 10;
            flow.BeginValidation();
            flow.SubmitValidation(false);
            flow.RegisterObserverIntervention();
            now = 40;
            flow.BeginValidation();
            flow.SubmitValidation(true);
            now = 55;
            flow.SetMode(GuidanceMode.Demo3D);
            flow.BeginValidation();
            flow.SubmitValidation(true);
            now = 100;

            Assert.IsTrue(summary.IsFinished);
            Assert.AreEqual(0, summary.StartedAtStep);
            Assert.AreEqual(2, summary.CompletedSteps);
            Assert.AreEqual(3, summary.ValidationAttempts);
            Assert.AreEqual(1, summary.FailedValidations);
            Assert.AreEqual(1, summary.ObserverInterventions);
            Assert.AreEqual(1, summary.ModeChanges);
            Assert.AreEqual(55, summary.ElapsedSeconds, 1e-9, "o tempo para no session_completed");
            Assert.AreEqual(2, summary.Steps.Count);
            Assert.AreEqual(40, summary.Steps[0].Seconds, 1e-9);
            Assert.AreEqual(1, summary.Steps[0].FailedValidations);
            Assert.AreEqual(15, summary.Steps[1].Seconds, 1e-9);
        }

        [Test]
        public void Summary_OnResume_StartsAtSavedStep()
        {
            var summary = new SessionSummary(() => 0);
            StepFlow flow = CreateFlow(new FakeStep("a", ""), new FakeStep("b", ""), new FakeStep("c", ""));
            flow.EventRaised += summary.Record;

            flow.Start(2);

            Assert.AreEqual(2, summary.StartedAtStep);
            Assert.AreEqual(1, summary.Steps.Count);
            Assert.Less(summary.Steps[0].Seconds, 0, "etapa em andamento ainda sem tempo");
        }

        [Test]
        public void FormatDuration()
        {
            Assert.AreEqual("0:05", SessionSummary.FormatDuration(4.6));
            Assert.AreEqual("12:30", SessionSummary.FormatDuration(750));
            Assert.AreEqual("1:02:03", SessionSummary.FormatDuration(3723));
            Assert.AreEqual("—", SessionSummary.FormatDuration(-1));
        }
    }
}
