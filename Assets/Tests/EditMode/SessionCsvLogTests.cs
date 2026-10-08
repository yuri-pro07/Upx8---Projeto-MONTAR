using System;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace MontAR.Tests
{
    public class SessionCsvLogTests
    {
        private static readonly DateTime FixedUtc = new DateTime(2026, 10, 20, 14, 30, 5, 123, DateTimeKind.Utc);

        private static string[] Lines(StringWriter writer) =>
            writer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        private static SessionCsvLog CreateLog(StringWriter writer, double clockSeconds = 12.3456, bool header = true)
        {
            var info = new SessionInfo("P01_20261020T143000", "P01", "0.2.0", "Samsung SM-A546E");
            return new SessionCsvLog(writer, info, () => clockSeconds, () => FixedUtc, header);
        }

        [Test]
        public void Header_IsWrittenOnlyWhenRequested()
        {
            var withHeader = new StringWriter();
            CreateLog(withHeader);
            Assert.AreEqual(SessionCsvLog.Header, Lines(withHeader)[0]);

            var appending = new StringWriter();
            CreateLog(appending, header: false);
            Assert.AreEqual(string.Empty, appending.ToString());
        }

        [Test]
        public void StepEvent_IsWrittenWithAllColumns()
        {
            var writer = new StringWriter();
            SessionCsvLog log = CreateLog(writer);

            log.Write(new FlowEvent(FlowEventType.ValidationPassed, 3, "ram_slots", "checked=2/2"), 59.94f);

            Assert.AreEqual(
                "P01_20261020T143000,P01,0.2.0,Samsung SM-A546E,3,ram_slots,validation_passed,12346,2026-10-20T14:30:05.123Z,59.9,checked=2/2",
                Lines(writer)[1]);
        }

        [Test]
        public void SessionEvent_HasEmptyStepIndex_AndMissingFpsIsEmpty()
        {
            var writer = new StringWriter();
            SessionCsvLog log = CreateLog(writer);

            log.Write(new FlowEvent(FlowEventType.SessionStarted, -1, "", ""), -1f);

            string[] columns = Lines(writer)[1].Split(',');
            Assert.AreEqual(11, columns.Length);
            Assert.AreEqual("", columns[4]);
            Assert.AreEqual("session_started", columns[6]);
            Assert.AreEqual("", columns[9]);
        }

        [Test]
        public void Numbers_UseDotDecimal_EvenInBrazilianCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
                var writer = new StringWriter();
                SessionCsvLog log = CreateLog(writer, clockSeconds: 1.5);

                log.WriteRow(0, "cpu", "fps_sample", 29.5f, "");

                StringAssert.Contains(",1500,", Lines(writer)[1]);
                StringAssert.Contains(",29.5,", Lines(writer)[1]);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void Escape_QuotesFieldsWithCommasQuotesAndNewlines()
        {
            Assert.AreEqual("simples", SessionCsvLog.Escape("simples"));
            Assert.AreEqual("", SessionCsvLog.Escape(null));
            Assert.AreEqual("\"a,b\"", SessionCsvLog.Escape("a,b"));
            Assert.AreEqual("\"disse \"\"ok\"\"\"", SessionCsvLog.Escape("disse \"ok\""));
            Assert.AreEqual("\"linha1\nlinha2\"", SessionCsvLog.Escape("linha1\nlinha2"));
        }
    }
}
