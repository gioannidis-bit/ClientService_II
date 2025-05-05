using System;
using System.Linq;
using System.Reflection;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Helpers;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Classes.Devices.IDBox;

public class IDBoxScanner : IScanner
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public IDBoxCMD device;

	public IConnection connection;

	private ConfigurationModel config;

	public IDBoxScanner(ConfigurationModel config)
	{
		this.config = config;
		device = new IDBoxCMD(config);
		device.SetText = Send;
	}

    // ClientService.Classes.Devices.IDBox/IDBoxScanner.cs
    public bool IsConnected()
    {
        try
        {
            // Έλεγχος αν το device είναι διαθέσιμο
            if (device == null)
            {
                return false;
            }

            // Επιπλέον λογική για έλεγχο της σειριακής θύρας
            if (!string.IsNullOrEmpty(config.ComPort))
            {
                string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();
                if (!availablePorts.Contains(config.ComPort, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

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
			logger.Info("IDBox Scanner Connected");
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
			logger.Info("disconnecting");
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
		try
		{
			ConnectionFactory factory = new ConcreteConnectionFactory();
			connection = factory.GetConnection(config);
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
	}

	public void Reconnect()
	{
		try
		{
			device.Dispose();
			device = null;
			device = new IDBoxCMD(config);
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
				connection.Send(1 + data);
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
