namespace SchoolApp.Service
{
    public interface IUserService
    {
        void PrintAllUsers();
        void PrintUsersByRole(string role);
        void PrintUsersInGroup(int groupId);
    }
}