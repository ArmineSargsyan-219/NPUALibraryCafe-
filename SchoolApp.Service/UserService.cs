using SchoolApp.Repository;

namespace SchoolApp.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public void PrintAllUsers()
        {
            var users = _repo.GetAll();
            foreach (var u in users)
                Console.WriteLine($"[{u.Id}] {u.FirstName} {u.LastName} - {u.Role}");
        }

        public void PrintUsersByRole(string role)
        {
            var users = _repo.GetByRole(role);
            Console.WriteLine($"\n--- {role}s ---");
            foreach (var u in users)
                Console.WriteLine($"  {u.FirstName} {u.LastName} ({u.Email})");
        }

        public void PrintUsersInGroup(int groupId)
        {
            var users = _repo.GetByGroupId(groupId);
            Console.WriteLine($"\n--- Users in Group {groupId} ---");
            foreach (var u in users)
                Console.WriteLine($"  {u.FirstName} {u.LastName}");
        }
    }
}