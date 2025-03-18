using MagicVilla_ClassLibrary.Models;

namespace MagicVilla_VillaAPI.Repository.IRepostiory
{
    public interface IVillaNumberRepository : IRepository<VillaNumber>
    {
        Task<VillaNumber> UpdateAsync(VillaNumber entity);
    }
}
