using System;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class ParticipantCodeAndFpsTests
    {
        [TestCase("P01", true)]
        [TestCase(" p01 ", true)]
        [TestCase("RA-GRUPO_2", true)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase(null, false)]
        [TestCase("João", false)]
        [TestCase("P 01", false)]
        [TestCase("../P01", false)]
        [TestCase("ABCDEFGHIJKLMNOPQ", false)]
        public void ParticipantCode_Validation(string code, bool expected)
        {
            Assert.AreEqual(expected, ParticipantCode.IsValid(code));
        }

        [Test]
        public void SessionId_CombinesNormalizedCodeAndUtcTime()
        {
            var utc = new DateTime(2026, 10, 20, 14, 30, 0, DateTimeKind.Utc);
            Assert.AreEqual("P01_20261020T143000", ParticipantCode.CreateSessionId(" p01", utc));
            Assert.Throws<ArgumentException>(() => ParticipantCode.CreateSessionId("João", utc));
        }

        [Test]
        public void FpsWindow_ReportsAverageAfterOneSecond()
        {
            var fps = new FpsWindow();
            Assert.Less(fps.Current, 0f);

            // 1/32 s é exato em float: 32 frames fecham a janela em exatamente 1 s.
            for (int i = 0; i < 31; i++)
                fps.Tick(1f / 32f);
            Assert.Less(fps.Current, 0f);

            fps.Tick(1f / 32f);
            Assert.AreEqual(32f, fps.Current, 0.001f);
        }
    }
}
