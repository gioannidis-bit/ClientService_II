using ClientService.Models.Enumerators;

namespace ClientService.Models.Base;

public class ConfigurationModel
{
	public ScannerTypeEnum ScannerType { get; set; }

	public RowSeparatosEnum RowSeparator { get; set; }

	public string ComPort { get; set; }

	public int RestartInterval { get; set; }

	public ServerConnectionTypeEnum ServerConnectionType { get; set; }

	public string ServerUrl { get; set; }

	public int ServerPort { get; set; }

	public string ServerCredentials { get; set; }

	public int StationId { get; set; }

	public int? PostToCloudDelay { get; set; }

	public bool Debug { get; set; }
}
