using SchoolApp.Models;

namespace SchoolApp.Repository
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<UserProfile> _users;
        private readonly List<UserGroupMembership> _memberships;

        public InMemoryUserRepository()
        {
            _users = new List<UserProfile>
            {
                new UserProfile { Id = 1, FirstName = "Arman", LastName = "Petrosyan", Email = "arman@example.com", Role = "Student", IsActive = true },
                new UserProfile { Id = 2, FirstName = "Karen", LastName = "Ghazaryan", Email = "karen@example.com", Role = "Teacher", IsActive = true }
            };
            _memberships = new List<UserGroupMembership>
            {
                new UserGroupMembership { Id = 1, UserProfileId = 1, GroupId = 1, IsPrimary = true }
            };
        }

        public IEnumerable<UserProfile> GetAll() => _users;

        public UserProfile GetById(int id) =>
            _users.FirstOrDefault(u => u.Id == id);

        public IEnumerable<UserProfile> GetByRole(string role) =>
            _users.Where(u => u.Role == role);

        public IEnumerable<UserProfile> GetByGroupId(int groupId)
        {
            var userIds = _memberships
                .Where(m => m.GroupId == groupId)
                .Select(m => m.UserProfileId);
            return _users.Where(u => userIds.Contains(u.Id));
        }
    }
}