using System.Collections.Generic;

namespace ClientService.Helpers.Interfaces;

public interface IWebApiClientHelper
{
	string GetRequest(string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic");

	string PostRequest<T>(T model, string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic");

	string PatchRequest<T>(T model, string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic");

	string PostFormUrlEncoded<TResult>(string url, IEnumerable<KeyValuePair<string, string>> postData, Dictionary<string, string> heads, out int returnCode, out string ErrorMess);
}
