using SchoolApp.Models;

namespace SchoolApp.Repository
{
    public interface IUserRepository
    {
        IEnumerable<UserProfile> GetAll();
        UserProfile GetById(int id);
        IEnumerable<UserProfile> GetByRole(string role);
        IEnumerable<UserProfile> GetByGroupId(int groupId);
    }
}