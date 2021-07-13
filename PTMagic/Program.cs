using System;
using System.Threading;
using System.Reflection;
using System.Security.Permissions;
using Core.Helper;
using Microsoft.Extensions.DependencyInjection;


[assembly: AssemblyVersion("2.5.12")]
[assembly: AssemblyProduct("PT Magic")]

namespace PTMagic
{
  class Program
  {
    // Create a logger
    private static LogHelper _log = ServiceHelper.BuildLoggerService().GetRequiredService<LogHelper>();

    // Main class entry
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
    public static void Main(string[] args)
    {
      // Register a global exception handler
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalUnhandledExceptionHandler);

      // Init PTMagic
      Core.Main.PTMagic ptMagic = new Core.Main.PTMagic(_log);
      ptMagic.CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;

      // Start process
      ptMagic.StartProcess();

      // Keep the app running
      for (; ; )
      {
        Thread.Sleep(10000);
      }
    }

    // Global unhandled exception handler
    private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
      Exception e = (Exception)args.ExceptionObject;

      if (args.IsTerminating)
      {
        _log.DoLogCritical("Unhandled fatal exception occurred: ", e);
      }
      else
      {
        _log.DoLogError("Unhandled fatal exception occurred: " + e.ToString());
      }
    }
  }
}
