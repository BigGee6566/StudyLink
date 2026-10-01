using System;
using System.Configuration;
using System.Threading;
using System.Web.Hosting;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Runs IMaintenanceService on a timer inside the web app: expires unpaid holds, sends due-date
    /// and overdue reminders, snapshots DailyMetrics and refreshes TutorStats. Registered with the
    /// hosting environment so IIS lets a run finish before recycling. Every step is idempotent.
    /// </summary>
    public sealed class BackgroundSweep : IRegisteredObject
    {
        private static BackgroundSweep _instance;
        private readonly Timer _timer;
        private int _running;

        private BackgroundSweep(TimeSpan interval)
        {
            _timer = new Timer(_ => Tick(), null, TimeSpan.FromMinutes(1), interval);
        }

        public static void Start()
        {
            if (_instance != null) return;
            var minutes = int.TryParse(ConfigurationManager.AppSettings["StudyLink:SweepMinutes"], out var m) && m > 0 ? m : 60;
            _instance = new BackgroundSweep(TimeSpan.FromMinutes(minutes));
            HostingEnvironment.RegisterObject(_instance);
        }

        public static void Stop() => _instance?.Stop(true);

        private void Tick()
        {
            if (Interlocked.Exchange(ref _running, 1) == 1) return; // never overlap
            try
            {
                using (var services = AppServices.Create())
                    services.Maintenance.Run();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, "BackgroundSweep");
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        public void Stop(bool immediate)
        {
            _timer.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }
    }
}