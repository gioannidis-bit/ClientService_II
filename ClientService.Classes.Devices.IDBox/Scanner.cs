using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace ClientService.Classes.Devices.IDBox;

public class Scanner
{
	private static SerialPort _com = new SerialPort();

	private static int _timeout = 5000;

	private static DelegateReadMRZ _delegate_read_mrz;

	private static readonly object _locker = new object();

	private static bool _received = false;

	private static string _last_response = "";

	private static bool _continuous_reading = false;

	private static bool _running = false;

	private static CommunicationMode _com_mode = CommunicationMode.USB_CDC;

	public bool ContinuousReading
	{
		get
		{
			return _continuous_reading;
		}
		set
		{
			_continuous_reading = value;
			set_continuous_reading(value);
		}
	}

	public Scanner(DelegateReadMRZ delegate_read_mrz)
	{
		_delegate_read_mrz = delegate_read_mrz;
	}

	public bool Connect(string port_name, CommunicationMode com_mode)
	{
		int baudrate = 9600;
		if (com_mode == CommunicationMode.UART_115200)
		{
			baudrate = 115200;
		}
		try
		{
			_com = new SerialPort(port_name, baudrate, Parity.None, 8);
			_com.ReadTimeout = 2000;
			_com.WriteTimeout = 2000;
			_com.Open();
			_com.DtrEnable = true;
			_com.DiscardInBuffer();
			_com.DiscardOutBuffer();
			_com.ReadExisting();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool Disconnect()
	{
		try
		{
			_com.Close();
			return true;
		}
		catch (Exception value)
		{
			Console.Write(value);
			return false;
		}
	}

	public bool IsConnected()
	{
		return _com.IsOpen;
	}

	private static void read_packet()
	{
		byte[] response = new byte[256];
		_running = true;
		do
		{
			try
			{
				int i;
				for (i = 0; i < _timeout; i += 10)
				{
					Thread.Sleep(10);
					if (_com.BytesToRead > 2)
					{
						break;
					}
				}
				if (_com.BytesToRead <= 2)
				{
					continue;
				}
				if (_com.Read(response, 0, 3) != 3)
				{
					throw new InvalidOperationException();
				}
				char cmd = (char)response[0];
				int length = response[1] * 256 + response[2];
				if (length > 0)
				{
					if (length > response.Length)
					{
						throw new Exception("Device communication error.");
					}
					for (; i < _timeout; i += 10)
					{
						Thread.Sleep(10);
						if (_com.BytesToRead >= length)
						{
							break;
						}
					}
					if (_com.Read(response, 0, length) != length)
					{
						throw new InvalidOperationException();
					}
				}
				switch (cmd)
				{
				case 'I':
					_last_response = Encoding.ASCII.GetString(response, 0, length).Replace("\r", "\r\n");
					if (_continuous_reading)
					{
						_delegate_read_mrz(_last_response);
						set_continuous_reading(state: true);
					}
					break;
				case 'M':
					if (length > 0)
					{
						_com_mode = (CommunicationMode)response[0];
					}
					break;
				case 'R':
				case 'T':
				case 'V':
				case 'W':
					_last_response = Encoding.ASCII.GetString(response, 0, length);
					break;
				}
				lock (_locker)
				{
					_received = true;
					Monitor.Pulse(_locker);
				}
			}
			catch
			{
			}
		}
		while (_continuous_reading);
		_running = false;
	}

	private static string send_cmd(byte[] cmd)
	{
		string response = "";
		int t = 0;
		try
		{
			_com.DiscardInBuffer();
			_com.Write(cmd, 0, cmd.Length);
			lock (_locker)
			{
				_last_response = "";
				_received = false;
				if (!_running)
				{
					new Thread(read_packet).Start();
				}
				while (!_received && t < _timeout)
				{
					Monitor.Wait(_locker, 1000);
					t += 1000;
				}
				if (_received)
				{
					response = _last_response;
				}
			}
		}
		catch
		{
		}
		return response;
	}

	private static void set_continuous_reading(bool state)
	{
		byte[] enable_reading = new byte[4] { 67, 0, 1, 0 };
		if (state)
		{
			enable_reading[3] = 1;
		}
		send_cmd(enable_reading);
	}

	public void Inquire()
	{
		string mrz = send_cmd(new byte[3] { 73, 0, 0 });
		_delegate_read_mrz(mrz);
	}

	public string GetVersion()
	{
		return send_cmd(new byte[3] { 86, 0, 0 });
	}

	public string GetOCRVersion()
	{
		return send_cmd(new byte[3] { 87, 0, 0 });
	}

	public string GetSerialNumber()
	{
		return send_cmd(new byte[3] { 82, 0, 0 });
	}

	public string GetProductInfo()
	{
		return send_cmd(new byte[3] { 84, 0, 0 });
	}

	public CommunicationMode GetCommunicationMode()
	{
		byte[] cmd = new byte[3] { 77, 0, 0 };
		_com_mode = CommunicationMode.NONE;
		send_cmd(cmd);
		return _com_mode;
	}

	public void SetCommunicationMode(CommunicationMode com_mode)
	{
		byte[] set_comm = new byte[7] { 77, 0, 4, 0, 0, 0, 0 };
		if (_com_mode != com_mode)
		{
			set_comm[3] = (byte)com_mode;
			if (com_mode == CommunicationMode.USB_HID_CDC)
			{
				set_comm[5] = 16;
			}
			send_cmd(set_comm);
			_com_mode = com_mode;
		}
	}
}
