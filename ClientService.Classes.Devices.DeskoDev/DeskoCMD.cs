using System;
using System.Reflection;
using System.Text;
using Desko.HidApi;
using log4net;

namespace ClientService.Classes.Devices.DeskoDev;

public class DeskoCMD : IDisposable
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public Device device;

	public Func<string, string> SetText { get; set; }

	public void Initialise()
	{
		try
		{
			device = new Device(shared: true);
			setListener();
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
	}

	private void setListener()
	{
		try
		{
			_ = HidApi.Instance;
			device.Disconnect();
			device = new Device(shared: false);
			device.Connect();
			device.ReadDataListener = delegate(Desko.HidApi.Module moduleType, byte[] readBuffer)
			{
				logger.Info("Desko listener triggered");
				if (moduleType == Desko.HidApi.Module.Ocr)
				{
					string arg = Encoding.Default.GetString(readBuffer);
					SetText(arg);
				}
			};
		}
		catch (HidApiException ex)
		{
			logger.Info("Error starting Desko listener: " + ex.Message);
		}
	}

	public void Release()
	{
		device.Disconnect();
		device.ReadDataListener = null;
		device = null;
	}

	public void Dispose()
	{
		Release();
	}

	~DeskoCMD()
	{
	}
}
