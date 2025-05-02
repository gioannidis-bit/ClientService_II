namespace ClientService.Models.Base;

public class DBConnectionModel
{
	public string ServerName { get; set; }

	public string DBName { get; set; }

	public string DBUser { get; set; }

	public string DBPass { get; set; }

	public int ConnectionTimeOut { get; set; }
}
