using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{

    public interface IHttpService
    {
        Task<HttpResult<TResponse>> PostJsonAsync<TResponse>(string jsonContent, string url);
        Task<HttpResult<TResponse>> PostAsJsonAsync<TRequest, TResponse>(TRequest content, string url);
        Task<HttpResult<TResponse>> SendAsync<TResponse>(HttpRequestMessage request);
        Task<HttpResult<TResponse>> SendAsyncPlainResponse<TResponse>(HttpRequestMessage request);
        
    }

}
