using System;
using System.Reflection;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Helpers;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Classes.Devices.DeskoDev;

public class DeskoScanner : IScanner
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public DeskoCMD device;

	public IConnection connection;

	public ConfigurationModel config;

	public DeskoScanner(ConfigurationModel config)
	{
		logger.Info("Contacting Desko Scanner");
		this.config = config;
		device = new DeskoCMD();
		device.SetText = Send;
	}

    // ClientService.Classes.Devices.DeskoDev/DeskoScanner.cs
    public bool IsConnected()
    {
        try
        {
            // Έλεγχος αν το device είναι διαθέσιμο
            if (device == null || device.device == null)
            {
                return false;
            }

            // Επιπλέον λογική ελέγχου αν απαιτείται
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Error checking scanner connection: {ex.Message}", ex);
            return false;
        }
    }

    public int Connect()
	{
		try
		{
			device.Initialise();
			logger.Info("Desko Scanner Connected");
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
			logger.Info("Desko Scanner Disconnected");
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
		return 0;
	}

	public void CreateConnection()
	{
		logger.Info("Desko Connecting To Server");
		ConnectionFactory factory = new ConcreteConnectionFactory();
		connection = factory.GetConnection(config);
	}

	public void Reconnect()
	{
		try
		{
			device.Dispose();
			device = null;
			device = new DeskoCMD();
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
