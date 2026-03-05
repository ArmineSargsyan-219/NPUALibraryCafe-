using SchoolApp.Models;
using System.Text.Json;

namespace SchoolApp.Repository
{
    public class FileUserRepository : IUserRepository
    {
        private readonly SchoolData _data;

        public FileUserRepository(string filePath)
        {
            var json = File.ReadAllText(filePath);
            _data = JsonSerializer.Deserialize<SchoolData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public IEnumerable<UserProfile> GetAll() => _data.UserProfiles;

        public UserProfile GetById(int id) =>
            _data.UserProfiles.FirstOrDefault(u => u.Id == id);

        public IEnumerable<UserProfile> GetByRole(string role) =>
            _data.UserProfiles.Where(u => u.Role == role);

        public IEnumerable<UserProfile> GetByGroupId(int groupId)
        {
            var userIds = _data.UserGroupMemberships
                .Where(m => m.GroupId == groupId)
                .Select(m => m.UserProfileId);
            return _data.UserProfiles.Where(u => userIds.Contains(u.Id));
        }
    }
}