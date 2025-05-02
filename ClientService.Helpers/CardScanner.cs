using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Mrz;

namespace ClientService.Helpers;

public class CardScanner
{
	public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	public const int WM_SETTEXT = 12;

	public const int TCM_SETCURFOCUS = 4912;

	public const int WM_COMMAND = 273;

	public const int CB_SETCURSEL = 334;

	public const int CB_SELECTSTRING = 333;

	public const int CBN_SELCHANGE = 1;

	public const int CB_FINDSTRINGEXACT = 344;

	public const int CB_FINDSTRING = 332;

	public const int WM_CHAR = 258;

	public const int WM_KEYDOWN = 256;

	public const int WM_KEYUP = 257;

	public const int VK_RETURN = 13;

	public const int WM_GETTEXT = 13;

	public const int WM_GETTEXTLENGTH = 14;

	public const int VK_DELETE = 46;

	public const int EM_SETSEL = 177;

	private string cardHolder = "";

	private string cardNum = "";

	private string cardDateTo = "";

	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SendMessage(IntPtr hWnd, int msg, int Param, string s);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

	[DllImport("User32.dll")]
	private static extern int SetForegroundWindow(IntPtr point);

	public CardScanner(MrzCardRecord data)
	{
		cardHolder = data.ownerName;
		cardNum = data.cardNum;
		cardDateTo = data.expDate;
	}

	public void ProtelCloudInstanceHandle(int delay)
	{
		Keyboard(delay);
	}

	public void Keyboard(int delay)
	{
		GetForegroundWindow();
		string textFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\ProtelCloudPageName.txt";
		try
		{
			if (!File.Exists(textFile))
			{
				using FileStream fs = File.Create(textFile);
				byte[] title = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes("protel Cloud Centre - Google Chrome");
				fs.Write(title, 0, title.Length);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		string text = File.ReadAllText(textFile);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "protel Cloud Centre - Google Chrome";
		}
		IntPtr WindowName = FindWindow(null, text);
		if (WindowName == IntPtr.Zero)
		{
			Utils.Log("chrome not found. cannot send card data");
			return;
		}
		SetForegroundWindow(WindowName);
		SendKeys.SendWait("{TAB}");
		sendCharsSequence(WindowName, cardNum, delay + 10);
		SendKeys.SendWait("{TAB}");
		SendKeys.SendWait("{TAB}");
		sendCharsSequence(WindowName, cardHolder, delay);
		SendKeys.SendWait("{TAB}");
		SendKeys.SendWait("{TAB}");
		sendCharsSequence(WindowName, cardDateTo, delay);
		SendKeys.SendWait("{TAB}");
	}

	public static void sendCharsSequence(IntPtr iptrHWndControl, string str, int delay)
	{
		SendMessage(iptrHWndControl, 177, 0, "0");
		for (int i = 0; i < str.ToCharArray().Length; i++)
		{
			SendMessage(iptrHWndControl, 256, str.ToCharArray()[i], null);
			Thread.Sleep(delay);
			SendMessage(iptrHWndControl, 258, str.ToCharArray()[i], null);
			SendMessage(iptrHWndControl, 257, str.ToCharArray()[i], null);
		}
	}
}
