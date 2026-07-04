using System.IO;
using System.Web;

namespace BikeRental
{
    // The application's nerve center. Startup, shutdown, and the occasional existential crisis.
    // Global.asax predates Startup.cs, which predates Program.cs, which now does both jobs.
    // The framework has opinions. They are still forming. Global.asax had none. It just ran.
    //
    // This class is still named WebApiApplication because renaming it requires updating the
    // Inherits attribute in Global.asax, and that file is XML-adjacent, and no one is in the mood.
    public class WebApiApplication : HttpApplication
    {
        private static FleetMonitor _fleetMonitor;

        protected void Application_Start()
        {
            // HttpRuntime.AppDomainAppPath gives the physical path of the web root.
            // Server.MapPath("~/") would also work but requires touching HttpContext.Current,
            // which technically exists here but is context-free and judges you for using it.
            // In ASP.NET Core, IWebHostEnvironment.ContentRootPath replaces both.
            // It is injected. Everything is injected. Nothing is static. Enjoy.
            var dataFolder = Path.Combine(HttpRuntime.AppDomainAppPath, "SampleData");
            ApplicationServices.Initialize(dataFolder);

            _fleetMonitor = new FleetMonitor(dataFolder, OnDataChanged);
            _fleetMonitor.Start();
        }

        protected void Application_End()
        {
            // IIS is recycling the AppDomain. Time to clean up the background thread.
            // This is called on a "best effort" basis. IIS defines "best effort" loosely.
            if (_fleetMonitor != null)
                _fleetMonitor.Dispose();
        }

        private static void OnDataChanged()
        {
            // A file changed. The application has been notified.
            // The application nods, processes this information, and moves on with its life.
        }
    }
}
