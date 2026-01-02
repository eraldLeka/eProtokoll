using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Afatet për dokumentet - Menaxhimi i afateve për kthim përgjigje dhe veprime të tjera
    /// </summary>
    public class Deadline
    {
        [Key]
        public int DeadlineId { get; set; }

        [Required(ErrorMessage = "Dokumenti është i detyrueshëm")]
        [Display(Name = "Dokumenti")]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        [Display(Name = "Dokumenti")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Titulli i afatit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Titulli i Afatit")]
        public string Title { get; set; }

        [StringLength(1000)]
        [Display(Name = "Përshkrimi")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Data e afatit është e detyrueshme")]
        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Ora e Afatit")]
        [DataType(DataType.Time)]
        public TimeSpan? DueTime { get; set; }

        [Required]
        [Display(Name = "Lloji i Afatit")]
        public DeadlineType Type { get; set; }

        [Required]
        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [StringLength(450)]
        [Display(Name = "Përgjegjësi")]
        public string? ResponsibleUserId { get; set; }

        [ForeignKey("ResponsibleUserId")]
        [Display(Name = "Përdoruesi Përgjegjës")]
        public virtual ApplicationUser? ResponsibleUser { get; set; }

        [StringLength(100)]
        [Display(Name = "Departamenti Përgjegjës")]
        public string? ResponsibleDepartment { get; set; }

        [Display(Name = "Statusi i Afatit")]
        public DeadlineStatus Status { get; set; } = DeadlineStatus.Pending;

        [Display(Name = "Data e Fillimit")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Është Përfunduar")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Data e Përfundimit")]
        public DateTime? CompletedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Përfunduar nga")]
        public string? CompletedBy { get; set; }

        [ForeignKey("CompletedBy")]
        public virtual ApplicationUser? CompletedByUser { get; set; }

        [StringLength(1000)]
        [Display(Name = "Koment i Përfundimit")]
        [DataType(DataType.MultilineText)]
        public string? CompletionNotes { get; set; }

        [Display(Name = "Përqindja e Përfundimit")]
        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;

        [Display(Name = "Është i Vonuar")]
        public bool IsOverdue
        {
            get
            {
                if (IsCompleted) return false;
                var deadline = DueTime.HasValue
                    ? DueDate.Add(DueTime.Value)
                    : DueDate.AddDays(1).AddSeconds(-1);
                return DateTime.Now > deadline;
            }
        }

        [Display(Name = "Ditë të Mbetura")]
        public int DaysRemaining
        {
            get
            {
                if (IsCompleted) return 0;
                var deadline = DueTime.HasValue
                    ? DueDate.Add(DueTime.Value)
                    : DueDate.AddDays(1).AddSeconds(-1);
                var span = deadline - DateTime.Now;
                return (int)Math.Ceiling(span.TotalDays);
            }
        }

        [Display(Name = "Dërgo Njoftim")]
        public bool SendNotification { get; set; } = true;

        [Display(Name = "Ditë Para Njoftimit")]
        [Range(0, 365)]
        public int NotificationDaysBefore { get; set; } = 3;

        [Display(Name = "Njoftimi është Dërguar")]
        public bool NotificationSent { get; set; } = false;

        [Display(Name = "Data e Njoftimit")]
        public DateTime? NotificationSentDate { get; set; }

        [Display(Name = "Dërgo Përkujtues")]
        public bool SendReminder { get; set; } = true;

        [Display(Name = "Intervali i Përkujtuesit (ditë)")]
        [Range(1, 30)]
        public int ReminderIntervalDays { get; set; } = 1;

        [Display(Name = "Përkujtuesi i Fundit")]
        public DateTime? LastReminderDate { get; set; }

        [Display(Name = "Numri i Përkujtuesve")]
        public int ReminderCount { get; set; } = 0;

        [Display(Name = "Eskalimi Automatik")]
        public bool AutoEscalate { get; set; } = false;

        [StringLength(450)]
        [Display(Name = "Eskalim tek")]
        public string? EscalateToUserId { get; set; }

        [ForeignKey("EscalateToUserId")]
        public virtual ApplicationUser? EscalateToUser { get; set; }

        [Display(Name = "Ditë para Eskalimit")]
        [Range(0, 30)]
        public int EscalateDaysBefore { get; set; } = 1;

        [Display(Name = "Është Eskaluar")]
        public bool IsEscalated { get; set; } = false;

        [Display(Name = "Data e Eskalimit")]
        public DateTime? EscalatedDate { get; set; }

        [Display(Name = "Është i Zgjatur")]
        public bool IsExtended { get; set; } = false;

        [Display(Name = "Afati Origjinal")]
        [DataType(DataType.Date)]
        public DateTime? OriginalDueDate { get; set; }

        [Display(Name = "Arsyeja e Zgjatjes")]
        [StringLength(500)]
        public string? ExtensionReason { get; set; }

        [StringLength(450)]
        [Display(Name = "Zgjatur nga")]
        public string? ExtendedBy { get; set; }

        [Display(Name = "Data e Zgjatjes")]
        public DateTime? ExtensionDate { get; set; }

        [Display(Name = "Kërkon Miratim")]
        public bool RequiresApproval { get; set; } = false;

        [Display(Name = "Është Miratuar")]
        public bool IsApproved { get; set; } = false;

        [StringLength(450)]
        [Display(Name = "Miratuar nga")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Data e Miratimit")]
        public DateTime? ApprovedDate { get; set; }

        [Display(Name = "Është Aktiv")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Krijuar nga")]
        public string CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// Llojet e afateve
    /// </summary>
    public enum DeadlineType
    {
        [Display(Name = "Afat Përgjigje")]
        Response = 1,

        [Display(Name = "Afat Veprimi")]
        Action = 2,

        [Display(Name = "Afat Miratimi")]
        Approval = 3,

        [Display(Name = "Afat Dorëzimi")]
        Submission = 4,

        [Display(Name = "Afat Rishikimi")]
        Review = 5,

        [Display(Name = "Afat Raportimi")]
        Reporting = 6,

        [Display(Name = "Afat Implementimi")]
        Implementation = 7,

        [Display(Name = "Afat Arkivimi")]
        Archiving = 8,

        [Display(Name = "Tjetër")]
        Other = 9
    }

    /// <summary>
    /// Statusi i afatit
    /// </summary>
    public enum DeadlineStatus
    {
        [Display(Name = "Në Pritje")]
        Pending = 1,

        [Display(Name = "Në Proces")]
        InProgress = 2,

        [Display(Name = "I Përfunduar")]
        Completed = 3,

        [Display(Name = "I Vonuar")]
        Overdue = 4,

        [Display(Name = "I Zgjatur")]
        Extended = 5,

        [Display(Name = "I Anulluar")]
        Cancelled = 6,

        [Display(Name = "I Eskaluar")]
        Escalated = 7
    }
}