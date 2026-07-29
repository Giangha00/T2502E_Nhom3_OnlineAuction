using System.Net;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Support;

public sealed class E2EHttpHelper : IDisposable
{
    readonly HttpClient _client;
    readonly CookieContainer _cookies = new();
    readonly string _baseUrl;

    public E2EHttpHelper(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            AllowAutoRedirect = true,
            UseCookies = true
        };
        _client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl + "/") };
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "OnlineAuction.E2ETests/1.0");
    }

    public bool IsAppRunning()
    {
        try
        {
            var response = _client.GetAsync("").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public HttpStatusCode GetStatus(string path)
    {
        var response = _client.GetAsync(path.TrimStart('/')).GetAwaiter().GetResult();
        return response.StatusCode;
    }

    public string GetString(string path)
    {
        var response = _client.GetAsync(path.TrimStart('/')).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public HttpResponseMessage PostForm(string path, IEnumerable<KeyValuePair<string, string>> form)
    {
        var content = new FormUrlEncodedContent(form);
        return _client.PostAsync(path.TrimStart('/'), content).GetAwaiter().GetResult();
    }

    public void ImportCookiesFromDriver(IWebDriver driver)
    {
        foreach (var cookie in driver.Manage().Cookies.AllCookies)
        {
            try
            {
                _cookies.Add(new Uri(_baseUrl), new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }
            catch
            {
                // Ignore domain mismatch cookies.
            }
        }
    }

    public void Dispose() => _client.Dispose();
}
