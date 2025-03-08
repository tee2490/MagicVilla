using MagicVilla_ClassLibrary.Models.Dto;
using MagicVilla_ClassLibrary.Models;

namespace MagicVilla_VillaAPI.Repository.IRepostiory
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO);
    }
}
