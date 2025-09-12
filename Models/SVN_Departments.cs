using System.ComponentModel.DataAnnotations;

namespace SigmaNotificationBackend.Models
{
    public class SVN_Departments
    {
        [Key]
        public int id_department { get; set; }

        public string name_department { get; set; } = string.Empty;

        public List<SVN_Users> Users { get; set; }
    }
}
