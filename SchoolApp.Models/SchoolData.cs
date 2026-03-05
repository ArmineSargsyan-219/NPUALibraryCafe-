namespace SchoolApp.Models
{
    public class SchoolData
    {
        public List<Department> Departments { get; set; }
        public List<Group> Groups { get; set; }
        public List<UserProfile> UserProfiles { get; set; }
        public List<UserGroupMembership> UserGroupMemberships { get; set; }
    }
}