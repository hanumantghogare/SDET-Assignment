using System;
using System.IO;
using System.Threading;

namespace BikeRental
{
    // Watches the data folder on a background thread like a security guard
    // who checks the door every 10 seconds and writes nothing in the incident log.
    // In the web app, he is hired in Application_Start and occasionally fired by IIS
    // AppDomain recycling without anyone telling him. He has made peace with this.
    //
    // Thread.Abort() stops working in .NET 5+. Good luck.
    internal sealed class FleetMonitor : IDisposable
    {
        private readonly string _dataFolder;
        private readonly Action _onDataChanged;
        private Thread _thread;
        private DateTime _lastChecked;

        public FleetMonitor(string dataFolder, Action onDataChanged)
        {
            _dataFolder    = dataFolder;
            _onDataChanged = onDataChanged;
        }

        // Clocks the guard in. He will now wake up every 10 seconds,
        // squint at some file timestamps, and go back to sleep.
        // Performance review: satisfactory.
        public void Start()
        {
            _lastChecked = DateTime.UtcNow;
            _thread = new Thread(Poll)
            {
                IsBackground = true,
                Name         = "FleetMonitor"
            };
            _thread.Start();
        }

        // The guard's actual rounds. Checks write times. Calls home if anything changed.
        // ThreadAbortException is the intended exit — thrown by Dispose(), caught here,
        // handled gracefully. This is the closest .NET 4.x got to cooperative cancellation.
        // It worked. Nobody is proud of it.
        private void Poll()
        {
            while (true)
            {
                Thread.Sleep(10000);

                try
                {
                    foreach (var file in Directory.GetFiles(_dataFolder))
                    {
                        if (File.GetLastWriteTimeUtc(file) > _lastChecked)
                        {
                            _lastChecked = DateTime.UtcNow;
                            if (_onDataChanged != null) _onDataChanged();
                            break;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    // Dispose() fired the tranquilizer dart. Reset the abort flag and exit cleanly.
                    // In .NET 5+ this catch block is unreachable because Thread.Abort() throws
                    // PlatformNotSupportedException at the call site instead. Very different vibe.
                    Thread.ResetAbort();
                    return;
                }
                catch { /* swallow filesystem hiccups. the guard is not paid enough to panic. */ }
            }
        }

        // Fires Thread.Abort() at the polling thread like a tranquilizer dart.
        // Elegant in .NET 4.8. Throws PlatformNotSupportedException in .NET 5+,
        // which is the framework saying "we talked about this" while you were busy ignoring the warnings.
        public void Dispose()
        {
            if (_thread != null && _thread.IsAlive)
                _thread.Abort();
        }
    }
}
