using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Gjurmimi i dokumenteve - Delegimi i dokumenteve tek punonjësit dhe gjurmimi i statusit
    /// </summary>
    public class DocumentTracking
    {
        [Key]
        public int TrackingId { get; set; }

        [Required(ErrorMessage = "Dokumenti është i detyrueshëm")]
        [Display(Name = "Dokumenti")]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        [Display(Name = "Dokumenti")]
        public virtual Document? Document { get; set; }

        [Required(ErrorMessage = "Përdoruesi i caktuar është i detyrueshëm")]
        [StringLength(450)]
        [Display(Name = "Caktuar Për")]
        public string AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        [Display(Name = "Përdoruesi")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [Required(ErrorMessage = "Përdoruesi që cakton është i detyrueshëm")]
        [StringLength(450)]
        [Display(Name = "Caktuar Nga")]
        public string AssignedByUserId { get; set; }

        [ForeignKey("AssignedByUserId")]
        [Display(Name = "Caktuar Nga")]
        public virtual ApplicationUser? AssignedByUser { get; set; }

        [Required]
        [Display(Name = "Data e Caktimit")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ora e Caktimit")]
        [DataType(DataType.Time)]
        public TimeSpan AssignedTime { get; set; } = DateTime.Now.TimeOfDay;

        [Required]
        [Display(Name = "Statusi i Gjurmimit")]
        public TrackingStatus Status { get; set; } = TrackingStatus.Assigned;

        [Required]
        [Display(Name = "Lloji i Veprimit")]
        public ActionType ActionType { get; set; }

        [Required(ErrorMessage = "Udhëzimet janë të detyrueshme")]
        [StringLength(1000)]
        [Display(Name = "Udhëzimet")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; }

        [Display(Name = "Prioriteti")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "Ka Afat")]
        public bool HasDeadline { get; set; } = false;

        [Display(Name = "Data e Afatit")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Kërkon Përgjigje")]
        public bool RequiresResponse { get; set; } = false;

        [Display(Name = "Kërkon Raport")]
        public bool RequiresReport { get; set; } = false;

        [Display(Name = "Është Lexuar")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Data e Leximit")]
        public DateTime? ReadDate { get; set; }

        [Display(Name = "Është Pranuar")]
        public bool IsAccepted { get; set; } = false;

        [Display(Name = "Data e Pranimit")]
        public DateTime? AcceptedDate { get; set; }

        [Display(Name = "Është në Proces")]
        public bool IsInProgress { get; set; } = false;

        [Display(Name = "Data e Fillimit")]
        public DateTime? StartedDate { get; set; }

        [Display(Name = "Është Përfunduar")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Data e Përfundimit")]
        public DateTime? CompletedDate { get; set; }

        [StringLength(2000)]
        [Display(Name = "Koment i Përfundimit")]
        [DataType(DataType.MultilineText)]
        public string? CompletionComment { get; set; }

        [Display(Name = "Përqindja e Përfundimit")]
        [Range(0, 100)]
        public int CompletionPercentage { get; set; } = 0;

        [Display(Name = "Është Refuzuar")]
        public bool IsRejected { get; set; } = false;

        [Display(Name = "Data e Refuzimit")]
        public DateTime? RejectedDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Arsyeja e Refuzimit")]
        [DataType(DataType.MultilineText)]
        public string? RejectionReason { get; set; }

        [Display(Name = "Është Deleguar Më Tej")]
        public bool IsDelegated { get; set; } = false;

        [Display(Name = "Deleguar Tek")]
        public int? DelegatedToTrackingId { get; set; }

        [ForeignKey("DelegatedToTrackingId")]
        public virtual DocumentTracking? DelegatedToTracking { get; set; }

        [Display(Name = "Gjurmimi Prind")]
        public int? ParentTrackingId { get; set; }

        [ForeignKey("ParentTrackingId")]
        public virtual DocumentTracking? ParentTracking { get; set; }

        [StringLength(1000)]
        [Display(Name = "Shënime")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Kohëzgjatja (orë)")]
        public decimal? DurationHours { get; set; }

        [Display(Name = "Është i Vonuar")]
        public bool IsOverdue { get; set; } = false;

        [Display(Name = "Është Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Numri Rendor")]
        public int SequenceNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Dokumentet e Bashkangjitura")]
        public string? AttachedFiles { get; set; }

        [Display(Name = "Data e Krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data e Modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }


        // Navigation Properties për delegimet e shumta
        public virtual ICollection<DocumentTracking>? SubDelegations { get; set; }
        [Display(Name = "Përgjigjet")]
        public virtual ICollection<DocumentResponse>? Responses { get; set; }
    }

    /// <summary>
    /// Statusi i gjurmimit të dokumentit
    /// </summary>
    public enum TrackingStatus
    {
        [Display(Name = "I Caktuar")]
        Assigned = 1,

        [Display(Name = "I Pranuar")]
        Accepted = 2,

        [Display(Name = "Në Proces")]
        InProgress = 3,

        [Display(Name = "Në Pritje")]
        Pending = 4,

        [Display(Name = "I Përfunduar")]
        Completed = 5,

        [Display(Name = "I Deleguar")]
        Delegated = 6,

        [Display(Name = "I Refuzuar")]
        Rejected = 7,

        [Display(Name = "I Anulluar")]
        Cancelled = 8
    }

    /// <summary>
    /// Llojet e veprimeve për gjurmim
    /// </summary>
    public enum ActionType
    {
        [Display(Name = "Për Informacion")]
        ForInformation = 1,

        [Display(Name = "Për Veprim")]
        ForAction = 2,

        [Display(Name = "Për Përgjigje")]
        ForResponse = 3,

        [Display(Name = "Për Miratim")]
        ForApproval = 4,

        [Display(Name = "Për Firmë")]
        ForSignature = 5,

        [Display(Name = "Për Rishikim")]
        ForReview = 6,

        [Display(Name = "Për Koment")]
        ForComment = 7,

        [Display(Name = "Për Arkivim")]
        ForArchiving = 8,

        [Display(Name = "Tjetër")]
        Other = 9
    }
}