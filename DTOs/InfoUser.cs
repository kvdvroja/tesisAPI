namespace tesisAPI.DTOs
{
    public class InfoUser
    {
        public InfoUser(int userId, string userUpaoId, string userName, string firstName, string lastName, string departmentOffice)
        {
            UserId = userId;
            UserUpaoId = userUpaoId;
            UserName = userName;
            FirstName = firstName;
            LastName = lastName;
            DepartmentOffice = departmentOffice;
        }

        public int UserId { get; set; }
        public string UserUpaoId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DepartmentOffice { get; set; }
    }
}
