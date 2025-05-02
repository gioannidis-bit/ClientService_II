using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using ClientService.Helpers;
using log4net;

namespace ClientService;

public class MainCode
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public string SERVICENAME = "DocumentScanner";

	public string ExecName = "DocumentScanner";

	public string sPath;

	public void Main()
	{
		string exName = Process.GetCurrentProcess().ProcessName;
		logger.Info(exName);
		if (!string.IsNullOrEmpty(exName))
		{
			ExecName = exName;
			SERVICENAME = ExecName;
		}
		if (!Environment.UserInteractive)
		{
			logger.Info("Application opened as Windows Service");
			using Service service = new Service(ExecName, SERVICENAME);
			ServiceBase.Run(service);
			return;
		}
		logger.Info("Application opened as Console Application");
		ServiceController sc = new ServiceController(SERVICENAME);
		bool isServicePresent = false;
		try
		{
			_ = sc.Status;
			isServicePresent = true;
		}
		catch
		{
		}
		ConsoleKeyInfo key;
		if (isServicePresent)
		{
			Console.WriteLine("Application is installed as Windows Service.");
			Console.WriteLine((sc.Status != ServiceControllerStatus.Running) ? "Service is stopped\n\n[S] to start Service " : "Service is running!\n\n[S] to stop Service ");
			Console.WriteLine("[U] to unistall as Windows Service ");
			Console.WriteLine("ANY other key to exit");
			key = Console.ReadKey(intercept: true);
			Console.Clear();
			switch (key.KeyChar)
			{
			case 'S':
			case 's':
				if (sc.Status == ServiceControllerStatus.Running && sc.CanStop)
				{
					sc.Stop();
					sc.WaitForStatus(ServiceControllerStatus.Stopped);
					Console.WriteLine("Windows Service stopped!\n\nPress any key to exit...");
					Console.ReadKey(intercept: true);
				}
				else if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
				{
					sc.Start();
					sc.WaitForStatus(ServiceControllerStatus.Running);
					Console.WriteLine("Windows Service is up and running!\n\nPress any key to exit...");
					Console.ReadKey(intercept: true);
				}
				break;
			case 'U':
			case 'u':
				try
				{
					if (sc.CanStop)
					{
						sc.Stop();
						sc.WaitForStatus(ServiceControllerStatus.Stopped);
					}
				}
				catch
				{
				}
				new ClientService.Helpers.ServiceInstaller().UninstallService(SERVICENAME);
				logger.Info("Application uninstalled as Windows Service");
				Console.WriteLine("Application uninstalled as Windows Service succesfully!\n\nPress any key to exit...");
				Console.ReadKey(intercept: true);
				break;
			}
			return;
		}
		Console.WriteLine("Application is not installed as Windows Service.");
		Console.WriteLine("");
		Console.WriteLine("[I] to install as Windows Service");
		Console.WriteLine("[R] to run as Console Application ");
		key = Console.ReadKey(intercept: true);
		Console.Clear();
		char keyChar = key.KeyChar;
		if ((uint)keyChar <= 82u)
		{
			if (keyChar != 'I')
			{
				_ = 82;
				return;
			}
		}
		else if (keyChar != 'i')
		{
			_ = 114;
			return;
		}
		logger.Info("Application installing as Windows Service");
		new ClientService.Helpers.ServiceInstaller().InstallService(Environment.CurrentDirectory + "\\" + ExecName + ".exe", SERVICENAME, SERVICENAME);
		Console.WriteLine("Application installed as Windows Service succesfully and is up and runnning!\n\nPress any key to exit...");
		Console.ReadKey(intercept: true);
	}
}
