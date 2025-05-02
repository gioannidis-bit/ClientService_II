using System;
using System.Runtime.InteropServices;

namespace ClientService.Helpers;

internal class ServiceInstaller
{
	private string _servicePath;

	private string _serviceName;

	private string _serviceDisplayName;

	[DllImport("advapi32.dll")]
	public static extern IntPtr OpenSCManager(string lpMachineName, string lpSCDB, int scParameter);

	[DllImport("Advapi32.dll")]
	public static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName, int dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName, string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

	[DllImport("advapi32.dll")]
	public static extern void CloseServiceHandle(IntPtr SCHANDLE);

	[DllImport("advapi32.dll")]
	public static extern int StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern IntPtr OpenService(IntPtr SCHANDLE, string lpSvcName, int dwNumServiceArgs);

	[DllImport("advapi32.dll")]
	public static extern int DeleteService(IntPtr SVHANDLE);

	[DllImport("kernel32.dll")]
	public static extern int GetLastError();

	public bool InstallService(string svcPath, string svcName, string svcDispName)
	{
		int SC_MANAGER_CREATE_SERVICE = 2;
		int SERVICE_WIN32_OWN_PROCESS = 16;
		int SERVICE_ERROR_NORMAL = 1;
		int SERVICE_QUERY_CONFIG = 1;
		int SERVICE_CHANGE_CONFIG = 2;
		int SERVICE_QUERY_STATUS = 4;
		int SERVICE_ENUMERATE_DEPENDENTS = 8;
		int SERVICE_START = 16;
		int SERVICE_STOP = 32;
		int SERVICE_PAUSE_CONTINUE = 64;
		int SERVICE_INTERROGATE = 128;
		int SERVICE_USER_DEFINED_CONTROL = 256;
		int SERVICE_ALL_ACCESS = 0xF0000 | SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_QUERY_STATUS | SERVICE_ENUMERATE_DEPENDENTS | SERVICE_START | SERVICE_STOP | SERVICE_PAUSE_CONTINUE | SERVICE_INTERROGATE | SERVICE_USER_DEFINED_CONTROL;
		int SERVICE_AUTO_START = 2;
		try
		{
			IntPtr sc_handle = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
			if (sc_handle.ToInt64() != 0L)
			{
				IntPtr sv_handle = CreateService(sc_handle, svcName, svcDispName, SERVICE_ALL_ACCESS, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, svcPath, null, 0, null, null, null);
				if (sv_handle.ToInt64() == 0L)
				{
					CloseServiceHandle(sc_handle);
					return false;
				}
				if (StartService(sv_handle, 0, null) == 0)
				{
					return false;
				}
				CloseServiceHandle(sc_handle);
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public bool UninstallService(string svcName)
	{
		int GENERIC_WRITE = 1073741824;
		IntPtr sc_hndl = OpenSCManager(null, null, GENERIC_WRITE);
		if (sc_hndl.ToInt64() != 0L)
		{
			int DELETE = 65536;
			IntPtr svc_hndl = OpenService(sc_hndl, svcName, DELETE);
			if (svc_hndl.ToInt64() != 0L)
			{
				if (DeleteService(svc_hndl) != 0)
				{
					CloseServiceHandle(sc_hndl);
					return true;
				}
				CloseServiceHandle(sc_hndl);
				return false;
			}
			return false;
		}
		return false;
	}
}
