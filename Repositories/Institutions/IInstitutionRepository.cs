using eProtokoll.Models;

namespace eProtokoll.Repositories.Institutions
{
    public interface IInstitutionRepository
    {
        Task<IEnumerable<Institution>> GetAllAsync();
        Task<Institution?> GetByIdAsync(int id);
        Task CreateAsync(Institution institution);
        Task UpdateAsync(Institution institution);
        Task DeleteAsync(int id);
        Task<int> GetDocumentCountAsync(int id);
    }
}