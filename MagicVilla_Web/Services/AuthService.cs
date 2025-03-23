using MagicVilla_ClassLibrary.Models.Dto;
using MagicVilla_ClassLibrary.Models;
using MagicVilla_ClassLibrary.Utility;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IBaseService _baseService;
        private string villaUrl;

        public AuthService(IHttpClientFactory clientFactory, IConfiguration configuration, IBaseService baseService)
        {
            _clientFactory = clientFactory;
            _baseService = baseService;
            villaUrl = configuration.GetValue<string>("ServiceUrls:VillaAPI");

        }

        public async Task<T> LoginAsync<T>(LoginRequestDTO obj)
        {
            return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/login"
            }, withBearer: false);
        }

        public async Task<T> RegisterAsync<T>(RegisterationRequestDTO obj)
        {
            return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/register"
            }, withBearer: false);
        }
    }
}