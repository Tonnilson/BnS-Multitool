using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProcessMonitoring
{
    public sealed class NetworkPerformanceReporter : IDisposable
    {
        private DateTime m_EtwStartTime;
        private TraceEventSession m_EtwSession;

        private readonly Counters m_Counters = new Counters();

        private class Counters
        {
            public long Received;
            public long Sent;
        }

        private NetworkPerformanceReporter() { }

        public static NetworkPerformanceReporter Create()
        {
            var networkPerformancePresenter = new NetworkPerformanceReporter();
            networkPerformancePresenter.Initialise();
            return networkPerformancePresenter;
        }

        private void Initialise()
        {
            // Note that the ETW class blocks processing messages, so should be run on a different thread if you want the application to remain responsive.
            Task.Run(() => StartEtwSession());
        }

        private void StartEtwSession()
        {
            try
            {
                var processId = Process.GetCurrentProcess().Id;
                ResetCounters();

                using (m_EtwSession = new TraceEventSession("MyKernelAndClrEventsSession"))
                {
                    m_EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    m_EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        if (data.ProcessID == processId)
                        {
                            lock (m_Counters)
                            {
                                m_Counters.Received += data.size;
                            }
                        }
                    };

                    m_EtwSession.Source.Kernel.TcpIpSend += data =>
                    {
                        if (data.ProcessID == processId)
                        {
                            lock (m_Counters)
                            {
                                m_Counters.Sent += data.size;
                            }
                        }
                    };

                    m_EtwSession.Source.Process();
                }
            }
            catch
            {
                ResetCounters(); // Stop reporting figures
                // Probably should log the exception
            }
        }

        public NetworkPerformanceData GetNetworkPerformanceData()
        {
            var timeDifferenceInSeconds = (DateTime.Now - m_EtwStartTime).TotalSeconds;

            NetworkPerformanceData networkData;

            lock (m_Counters)
            {
                networkData = new NetworkPerformanceData
                {
                    BytesReceived = Convert.ToInt64(m_Counters.Received / timeDifferenceInSeconds),
                    BytesSent = Convert.ToInt64(m_Counters.Sent / timeDifferenceInSeconds)
                };

            }

            // Reset the counters to get a fresh reading for next time this is called.
            ResetCounters();

            return networkData;
        }

        private void ResetCounters()
        {
            lock (m_Counters)
            {
                m_Counters.Sent = 0;
                m_Counters.Received = 0;
            }
            m_EtwStartTime = DateTime.Now;
        }

        public void Dispose()
        {
            m_EtwSession?.Dispose();
        }
    }

    public sealed class NetworkPerformanceData
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
    }
}