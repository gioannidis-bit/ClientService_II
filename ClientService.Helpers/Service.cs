using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Helpers;

internal class Service : ServiceBase
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private string ExecName;

	private string SERVICENAME;

	public static ConfigurationModel config;

	public static IScanner scanner;

	private IContainer components;

	public Service(string ExecName, string SERVICENAME)
	{
		string exName = Process.GetCurrentProcess().ProcessName;
		logger.Info(exName);
		if (!string.IsNullOrEmpty(exName))
		{
			this.ExecName = ExecName;
			this.SERVICENAME = SERVICENAME;
		}
		base.ServiceName = SERVICENAME;
	}

	protected override void OnStart(string[] args)
	{
		Start(args);
	}

	protected override void OnStop()
	{
		Stop();
	}

	public void Start(string[] args)
	{
		logger.Info("Initializing Protel Document Scanner");
		InitializeConfiguration();
		InitializeDevice();
	}

	public new void Stop()
	{
		if (!Environment.UserInteractive)
		{
			logger.Info("Application stopped as Windows Service");
		}
		else
		{
			logger.Info("Application closed as Console Application");
		}
	}

	public static void InitializeConfiguration()
	{
		logger.Info("Initializing Configuration");
		config = new ConfigurationHelper().GetConfiguration();
	}

	public static void InitializeDevice()
	{
		logger.Info("Initializing Device");
		scanner = new ConcreteScannerFactory().GetScanner(config);
		scanner.Connect();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		components = new Container();
		base.ServiceName = "Service";
	}
}
