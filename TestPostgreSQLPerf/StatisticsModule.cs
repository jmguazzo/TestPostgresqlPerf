using System;
using System.Collections.Concurrent;
using System.Linq;
namespace TestPostgreSQLPerf
{
    internal class StatisticsModule
    {
        private static TimeLog NegativeTimeSpan;
        internal ConcurrentBag<int> listrunning;
        internal ConcurrentBag<TimeLog> Durations;
        internal ConcurrentBag<string> Errors;

        static object lockObject = new object();
        internal int running = 0;
        internal int error = 0;
        internal int total = 0;
        internal int finished = 0;
        internal int timeout = 0;
        internal int ExClient = 0;

        public struct TimeLog
        {
            public TimeSpan Duration;
            public DateTime When;
        }

        public StatisticsModule()
        {
            NegativeTimeSpan = new TimeLog() { Duration = new TimeSpan(-1) };
            Durations = new ConcurrentBag<TimeLog>();
            listrunning = new ConcurrentBag<int>();
            Errors = new ConcurrentBag<string>();
        }

        internal void ExportStatistics()
        {
            string guidFilename = Guid.NewGuid().ToString();
            string filenameSummary = "summary." + guidFilename + ".txt";
            string filenameDetails = "details." + guidFilename + ".txt";
            System.IO.File.AppendAllText(filenameSummary, string.Format("{0}\r\n", guidFilename));
            System.IO.File.AppendAllText(filenameSummary, "Rung\tFinshd\tTotal\tTimeout\tError\tExClient\r\n");
            System.IO.File.AppendAllText(filenameSummary, string.Format("{0:0000}\t{1:0000}\t{2:0000}\t{3:0000}\t{4:0000}\t{5:0000}\r\n", running, finished, total, timeout, error, ExClient));

            TimeSpan totalduration = new TimeSpan(0);
            TimeSpan minimumduration = new TimeSpan(24000000);
            TimeSpan maximumduration = NegativeTimeSpan.Duration;

            for (int i = 0; i < Durations.Count; i++)
            {

                var actualDuration = Durations.ElementAt(i);

                if (actualDuration.Duration > NegativeTimeSpan.Duration)
                {
                    totalduration += actualDuration.Duration;
                    if (actualDuration.Duration < minimumduration)
                        minimumduration = actualDuration.Duration;
                    if (actualDuration.Duration > maximumduration)
                        maximumduration = actualDuration.Duration;
                }

                System.IO.File.AppendAllText(filenameDetails, string.Format("{0}\t{1}\t{2}\t{3}\r\n",
                    guidFilename,
                    listrunning.ElementAt(i),
                    actualDuration.Duration.Ticks,
                    actualDuration.When.ToString("HH:mm:sss")));

            }

            var average = new TimeSpan(totalduration.Ticks / Durations.Count);
            System.IO.File.AppendAllText(filenameSummary, string.Format("Minimum Duration:{0}\r\n", minimumduration));
            System.IO.File.AppendAllText(filenameSummary, string.Format("Maximum Duration:{0}\r\n", maximumduration));
            System.IO.File.AppendAllText(filenameSummary, string.Format("Average Duration:{0}\r\n", average));
            for (int i = 0; i < Errors.Count; i++)
            {
                System.IO.File.AppendAllText(filenameSummary, string.Format("{0}\r\n", Errors.ElementAt(i)));
            }
        }

        internal void AddError(string message)
        {
            Errors.Add(message);
            error++;
            AddNegativeTimespan();
        }


        internal void IncreaseRunningFinished()
        {
            lock (lockObject) { running--; finished++; }
        }

        internal void AddTooManyClients(string message)
        {
            Errors.Add(message);
            ExClient++;
            AddNegativeTimespan();
        }

        internal void AddTimeOut(string message)
        {
            Errors.Add(message);
            timeout++;
            AddNegativeTimespan();
        }
        internal void AddNegativeTimespan()
        {
            lock (lockObject)
            {
                Durations.Add(NegativeTimeSpan);
            }

        }

        internal void AddDurationNow(DateTime start)
        {
            lock (lockObject)
            {
                var now = DateTime.Now;
                Durations.Add(new TimeLog() { Duration = now - start, When = now });
            }
        }

        internal void LogInfo()
        {
            lock (lockObject)
            {
                Console.Write("{0:0000}\t{1:0000}\t{2:0000}\t{3:0000}\t{4:0000}\t{5:0000}", running, finished, total, timeout, error, ExClient);

                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
            }
        }

        internal void IncreaseRunning()
        {
            lock (lockObject)
            {
                running++;
            }
            listrunning.Add(running);
        }

        internal void IncreaseTotal()
        {
            lock (lockObject)
            {
                total++;
            }
        }
    }
}