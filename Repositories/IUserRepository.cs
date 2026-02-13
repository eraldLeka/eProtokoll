using eProtokoll.Models;

namespace eProtokoll.Repositories
{
    public interface IUserRepository
    {
        Task<Users?> GetByUsernameAsync(string username);
        Task<Users?> GetByIdAsync(int id);
        Task<IEnumerable<Users>> GetAllAsync();
        Task CreateAsync(Users user);
        Task UpdateAsync(Users user);
        Task DeleteAsync(int id);
    }
}