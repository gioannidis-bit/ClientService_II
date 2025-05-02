using System;
using System.Reflection;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Helpers;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Classes.Devices.AccessIS;

public class AccessISScanner : IScanner
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public AccessISCMD device;

	public IConnection connection;

	private ConfigurationModel config;

	public AccessISScanner(ConfigurationModel config)
	{
		this.config = config;
		device = new AccessISCMD();
		device.SetText = Send;
	}

	public int Connect()
	{
		try
		{
			device.Initialise();
			logger.Info("AccessIS Scanner Connected");
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
		return 0;
	}

	public int Disconnect()
	{
		try
		{
			device.Release();
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
		return 0;
	}

	public void CreateConnection()
	{
		ConnectionFactory factory = new ConcreteConnectionFactory();
		connection = factory.GetConnection(config);
	}

	public void Reconnect()
	{
		try
		{
			device.Dispose();
			device = null;
			device = new AccessISCMD();
			device.SetText = Send;
			device.Initialise();
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
	}

	public string Send(string data)
	{
		if (config.Debug)
		{
			logger.Info("Sending Data: " + data);
		}
		try
		{
			if (data.ToString().StartsWith("C") && !data.ToString().ToLower().StartsWith("c<ita") && !data.ToString().ToLower().StartsWith("ca"))
			{
				new CardToCloudHelper().SendToCloud(data, config.RowSeparator, config.PostToCloudDelay ?? 20);
			}
			else
			{
				CreateConnection();
				connection.Send((int)config.RowSeparator + data);
				Reconnect();
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
		finally
		{
			if (connection != null)
			{
				connection.Dispose();
				connection = null;
			}
		}
		return string.Empty;
	}
}
