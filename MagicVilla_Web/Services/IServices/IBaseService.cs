using MagicVilla_ClassLibrary.Models;

namespace MagicVilla_Web.Services.IServices
{
    public interface IBaseService
    {
        APIResponse responseModel { get; set; }
        Task<T> SendAsync<T>(APIRequest apiRequest);
    }
}
