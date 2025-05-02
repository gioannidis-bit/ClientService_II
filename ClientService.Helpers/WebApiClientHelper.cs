using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ClientService.Helpers.Interfaces;

namespace ClientService.Helpers;

public class WebApiClientHelper : IWebApiClientHelper
{
	private class HttpMessage
	{
		public string Message { get; set; }
	}

	public string GetRequest(string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic")
	{
		string result = null;
		HttpRequestMessage request = new HttpRequestMessage();
		request.RequestUri = new Uri(url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
		request.Method = HttpMethod.Get;
		lock (request)
		{
			using HttpClient client = new HttpClient();
			setHeaders(client, authenticationType, user, headers);
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			using HttpResponseMessage response = client.SendAsync(request).Result;
			Task<string> readAsStringAsync = response.Content.ReadAsStringAsync();
			returnCode = response.StatusCode.GetHashCode();
			if (returnCode == 200)
			{
				ErrorMess = "";
				result = readAsStringAsync.Result;
			}
			else
			{
				ErrorMess = readAsStringAsync.Result;
			}
		}
		request.Dispose();
		return result;
	}

	public string PostRequest<T>(T model, string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic")
	{
		MediaTypeFormatter formatter = ((!(mediaType == "application/json")) ? ((MediaTypeFormatter)new XmlMediaTypeFormatter()) : ((MediaTypeFormatter)new JsonMediaTypeFormatter()));
		string result = null;
		HttpRequestMessage request = new HttpRequestMessage
		{
			RequestUri = new Uri(url),
			Method = HttpMethod.Post,
			Content = new ObjectContent<T>(model, formatter)
		};
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		using (HttpClient client = new HttpClient())
		{
			setHeaders(client, authenticationType, user, headers);
			using HttpResponseMessage response = client.SendAsync(request).Result;
			Task<string> readAsStringAsync = response.Content.ReadAsStringAsync();
			returnCode = response.StatusCode.GetHashCode();
			if (returnCode >= 200 && returnCode <= 299)
			{
				ErrorMess = "";
				result = readAsStringAsync.Result;
			}
			else
			{
				ErrorMess = readAsStringAsync.Result;
			}
		}
		request.Dispose();
		return result;
	}

	public async Task<string> PostRequestAsync<T>(T model, string url, string user, Dictionary<string, string> headers, string mediaType = "application/json", string authenticationType = "Basic")
	{
		await Task.Run(delegate
		{
			string text = mediaType;
			MediaTypeFormatter formatter = ((!(text == "application/json")) ? ((MediaTypeFormatter)new XmlMediaTypeFormatter()) : ((MediaTypeFormatter)new JsonMediaTypeFormatter()));
			string result = null;
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage
			{
				RequestUri = new Uri(url),
				Method = HttpMethod.Post,
				Content = new ObjectContent<T>(model, formatter)
			};
			httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			using (HttpClient httpClient = new HttpClient())
			{
				setHeaders(httpClient, authenticationType, user, headers);
				using HttpResponseMessage httpResponseMessage = httpClient.SendAsync(httpRequestMessage).Result;
				Task<string> task = httpResponseMessage.Content.ReadAsStringAsync();
				int hashCode = httpResponseMessage.StatusCode.GetHashCode();
				result = ((hashCode < 200 || hashCode > 299) ? task.Result : task.Result);
			}
			httpRequestMessage.Dispose();
			return result;
		});
		return string.Empty;
	}

	public string PatchRequest<T>(T model, string url, string user, Dictionary<string, string> headers, out int returnCode, out string ErrorMess, string mediaType = "application/json", string authenticationType = "Basic")
	{
		string result = null;
		HttpRequestMessage request = new HttpRequestMessage();
		request.RequestUri = new Uri(url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
		request.Method = new HttpMethod("PATCH");
		MediaTypeFormatter formatter = ((!(mediaType == "application/json")) ? ((MediaTypeFormatter)new XmlMediaTypeFormatter()) : ((MediaTypeFormatter)new JsonMediaTypeFormatter()));
		request.Content = new ObjectContent<T>(model, formatter);
		using (HttpClient client = new HttpClient())
		{
			setHeaders(client, authenticationType, user, headers);
			using HttpResponseMessage response = client.SendAsync(request).Result;
			Task<string> readAsStringAsync = response.Content.ReadAsStringAsync();
			returnCode = response.StatusCode.GetHashCode();
			if (returnCode >= 200 && returnCode <= 299)
			{
				ErrorMess = "";
				result = readAsStringAsync.Result;
			}
			else
			{
				ErrorMess = readAsStringAsync.Result;
			}
		}
		request.Dispose();
		return result;
	}

	private void setHeaders(HttpClient client, string authenticationType, string user, Dictionary<string, string> headers)
	{
		if (!string.IsNullOrEmpty(user))
		{
			if (!(authenticationType == "Basic"))
			{
				if (authenticationType == "OAuth2")
				{
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", user);
				}
			}
			else
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(user)));
			}
		}
		if (headers == null)
		{
			return;
		}
		foreach (string key in headers.Keys)
		{
			if (key != null && headers[key] != null)
			{
				client.DefaultRequestHeaders.Add(key, headers[key]);
			}
		}
	}

	public string PostFormUrlEncoded<TResult>(string url, IEnumerable<KeyValuePair<string, string>> postData, Dictionary<string, string> heads, out int returnCode, out string ErrorMess)
	{
		try
		{
			string result = null;
			using HttpClient httpClient = new HttpClient();
			using FormUrlEncodedContent content = new FormUrlEncodedContent(postData);
			if (heads != null)
			{
				KeyValuePair<string, string>[] array = heads.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<string, string> kvp = array[i];
					if (kvp.Key != null && kvp.Value != null)
					{
						if (kvp.Key.Equals("Authorization"))
						{
							httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kvp.Value);
						}
						else
						{
							content.Headers.ContentType = new MediaTypeHeaderValue(kvp.Value.ToString());
						}
					}
				}
			}
			using (HttpResponseMessage res = httpClient.PostAsync(url, content).Result)
			{
				Task<string> readAsStringAsync = res.Content.ReadAsStringAsync();
				returnCode = res.StatusCode.GetHashCode();
				if (returnCode >= 200 && returnCode <= 299)
				{
					ErrorMess = "";
					result = readAsStringAsync.Result;
				}
				else
				{
					ErrorMess = readAsStringAsync.Result;
				}
			}
			content.Dispose();
			return result;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}
}
