using ClientService.Models.Enumerators;

namespace ClientService.Models;

public class ArgsModel
{
	public string data { get; set; }

	public RowSeparatosEnum separator { get; set; }

	public int delay { get; set; }
}
