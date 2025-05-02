using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using log4net;

namespace ClientService.Classes.Devices.AccessIS;

public class AccessISCMD : IDisposable
{
	private delegate void updateTextBox(string text);

	private delegate void updateDisplayMrz(int status);

	private delegate void updatedReaderButtonStatus(bool enableReader, bool disableReader);

	private delegate void disableButtons();

	private delegate void msrDelegate(ref uint Parameter, [MarshalAs(UnmanagedType.LPStr)] StringBuilder data, int dataSize);

	private delegate void msrConnectionDelegate(ref uint Parameter, bool connectionStatus);

	private enum PacketType
	{
		MRZ,
		MSR
	}

	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private msrDelegate msrData;

	private msrConnectionDelegate msrDataConnection;

	private const string DLL_LOCATION = "Access_IS_MSR.dll";

	public Func<string, string> SetText { get; set; }

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
	private static extern void initialiseMsr(bool managedCode);

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern void msrRelease();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern bool enableMSR();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern bool disableMSR();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern PacketType getPacketType();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	private static extern string getDeviceName();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern int getMrzFailureStatus();

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall)]
	private static extern bool registerMSRCallback(msrDelegate Callback, ref uint Parameter);

	[DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	private static extern bool registerMSRConnectionCallback(msrConnectionDelegate Callback, ref uint Parameter);

	public void Initialise()
	{
		try
		{
			uint Val = 0u;
			initialiseMsr(managedCode: true);
			msrData = MsrCallback;
			msrDataConnection = MsrConnectionCallback;
			registerMSRCallback(msrData, ref Val);
			registerMSRConnectionCallback(msrDataConnection, ref Val);
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
	}

	public void Release()
	{
		uint Val = 0u;
		registerMSRCallback(null, ref Val);
		registerMSRConnectionCallback(null, ref Val);
		msrData = null;
		msrDataConnection = null;
	}

	private void MsrCallback(ref uint Parameter, [MarshalAs(UnmanagedType.LPStr)] StringBuilder data, int dataSize)
	{
		logger.Info("AccessIS listener triggered");
		SetText(data.ToString());
	}

	private void MsrConnectionCallback(ref uint Parameter, bool connectionStatus)
	{
	}

	public void Dispose()
	{
		Release();
	}

	~AccessISCMD()
	{
	}
}
