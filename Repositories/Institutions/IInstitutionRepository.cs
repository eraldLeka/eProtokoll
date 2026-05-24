using eProtokoll.Models;

namespace eProtokoll.Repositories.Institutions
{
    public interface IInstitutionRepository
    {
        Task<IEnumerable<Institution>> GetAllAsync();
        Task<Institution?> GetByIdAsync(int id);
        Task CreateAsync(Institution institution);
        Task UpdateAsync(Institution institution);
        Task DeactivateAsync(int id, string? modifiedBy);
        Task ActivateAsync(int id, string? modifiedBy);
        Task<int> GetDocumentCountAsync(int id);
    }
}