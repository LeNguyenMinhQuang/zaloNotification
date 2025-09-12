using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.SqlClient;
using SigmaNotificationBackend.Models;

public class SVN_Messages
{
    [Key]
    public int id_message { get; set; }
    [Required]
    public string operation { get; set; }

    [Required]
    public string content { get; set; }

    [Required]
    public string type_message { get; set; }

    public DateTime create_at { get; set; }

    public string UserId { get; set; }

    public int isSent { get; set; }

    [ForeignKey("Department")]
    public int to_department { get; set; }

    public SVN_Departments? Department { get; set; }

    public string? isOperatorSeen { get; set; }
    public string? isSupervisorSeen { get; set; }
    public string? isManagerSeen { get; set; }



}

