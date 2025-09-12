using System.ComponentModel.DataAnnotations;

namespace SigmaNotificationBackend.Models
{
    public class SVN_Users
    {

        [Key]
        public int id_user { get; set; }

        public string svn_number { get; set; }

        public string name_user { get; set; }

        public string password_user { get; set; }

        public int id_department { get; set; }

        public string? img_user { get; set; }

        // Thêm navigation property
        public SVN_Departments Department { get; set; }
    }
}
