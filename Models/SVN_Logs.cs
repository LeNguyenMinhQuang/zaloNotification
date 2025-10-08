using SigmaNotificationBackend.Models;
using System.ComponentModel.DataAnnotations;
    public class SVN_Logs
    {
        [Key]
        public int id_log { get; set; }

        public int id_message { get; set; }
        public SVN_Messages? Message { get; set; }

        public int id_user { get; set; }
        public SVN_Users? User { get; set; }

        public string status { get; set; }
    }

