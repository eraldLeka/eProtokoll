using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet e brendshme - Shkresa që qarkullojnë brenda institucionit
    /// </summary>
    public class InternalDocument : Document
    {
        [Required(ErrorMessage = "Departamenti dërgues është i detyrueshëm")]
        [StringLength(100)]
        [Display(Name = "Departamenti Dërgues")]
        public string FromDepartment { get; set; }

        [Required(ErrorMessage = "Departamenti marrës është i detyrueshëm")]
        [StringLength(100)]
        [Display(Name = "Departamenti Marrës")]
        public string ToDepartment { get; set; }

        [Required(ErrorMessage = "Lloji i dokumentit të brendshëm është i detyrueshëm")]
        [Display(Name = "Lloji i Dokumentit")]
        public InternalDocumentType InternalType { get; set; }

        [StringLength(450)]
        [Display(Name = "Nga (Përdoruesi)")]
        public string? FromUserId { get; set; }

        [ForeignKey("FromUserId")]
        public virtual ApplicationUser? FromUser { get; set; }

        [StringLength(450)]
        [Display(Name = "Për (Përdoruesi)")]
        public string? ToUserId { get; set; }

        [ForeignKey("ToUserId")]
        public virtual ApplicationUser? ToUser { get; set; }

        [StringLength(1000)]
        [Display(Name = "Lista e Marrësve (CC)")]
        public string? CarbonCopyList { get; set; }

        [Display(Name = "Kërkon Përgjigje")]
        public bool RequiresResponse { get; set; } = false;

        [Display(Name = "Data e Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDeadline { get; set; }

        [Display(Name = "Është Përgjigjur")]
        public bool IsResponded { get; set; } = false;

        [Display(Name = "Data e Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDate { get; set; }

        [Display(Name = "Numri i Dokumentit të Përgjigjes")]
        public int? ResponseDocumentId { get; set; }

        [ForeignKey("ResponseDocumentId")]
        public virtual InternalDocument? ResponseDocument { get; set; }

        [Display(Name = "Kërkon Miratim")]
        public bool RequiresApproval { get; set; } = false;

        [Display(Name = "Është Miratuar")]
        public bool IsApproved { get; set; } = false;

        [StringLength(450)]
        [Display(Name = "Miratuar nga")]
        public string? ApprovedBy { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual ApplicationUser? Approver { get; set; }

        [Display(Name = "Data e Miratimit")]
        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Koment i Miratimit")]
        [DataType(DataType.MultilineText)]
        public string? ApprovalComment { get; set; }

        [Display(Name = "Kërkon Firmë")]
        public bool RequiresSignature { get; set; } = false;

        [StringLength(450)]
        [Display(Name = "Firmosur nga")]
        public string? SignedBy { get; set; }

        [ForeignKey("SignedBy")]
        public virtual ApplicationUser? Signer { get; set; }

        [Display(Name = "Data e Firmosjes")]
        public DateTime? SignedDate { get; set; }

        [Display(Name = "Ka Firmë Digjitale")]
        public bool HasDigitalSignature { get; set; } = false;

        [Display(Name = "Është Lexuar")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Data e Leximit")]
        public DateTime? ReadDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Lexuar nga")]
        public string? ReadBy { get; set; }

        [Display(Name = "Kërkon Vëmendje")]
        public bool RequiresAttention { get; set; } = false;

        [Display(Name = "Është Konfidencial")]
        public bool IsConfidential { get; set; } = false;

        [Display(Name = "Numri i Kopjeve")]
        public int NumberOfCopies { get; set; } = 1;

        [Display(Name = "Ka Kopje Fizike")]
        public bool HasPhysicalCopy { get; set; } = false;

        [StringLength(100)]
        [Display(Name = "Vendndodhja Fizike")]
        public string? PhysicalLocation { get; set; }

        [Display(Name = "Qarkullim i Brendshëm")]
        public bool IsCirculation { get; set; } = false;

        [Display(Name = "Numri Rendor i Qarkullimit")]
        public int? CirculationOrder { get; set; }

        [StringLength(1000)]
        [Display(Name = "Lista e Qarkullimit")]
        public string? CirculationList { get; set; }

        [StringLength(100)]
        [Display(Name = "Numri i Referencës së Brendshme")]
        public string? InternalReferenceNumber { get; set; }

        [Display(Name = "Dokumenti Lidhur")]
        public int? RelatedDocumentId { get; set; }

        [ForeignKey("RelatedDocumentId")]
        public virtual InternalDocument? RelatedDocument { get; set; }

        [StringLength(500)]
        [Display(Name = "Veprim i Kërkuar")]
        public string? ActionRequired { get; set; }

        [Display(Name = "Data e Veprimtarisë")]
        [DataType(DataType.Date)]
        public DateTime? ActionDate { get; set; }

        [Display(Name = "Veprimi është Kryer")]
        public bool ActionCompleted { get; set; } = false;

        [Display(Name = "Data e Përfundimit të Veprimit")]
        public DateTime? ActionCompletedDate { get; set; }

        public InternalDocument()
        {
            DocumentType = DocumentType.Internal;
        }
    }

    /// <summary>
    /// Llojet e dokumenteve të brendshme
    /// </summary>
    public enum InternalDocumentType
    {
        [Display(Name = "Shënim")]
        Memo = 1,

        [Display(Name = "Raport")]
        Report = 2,

        [Display(Name = "Urdhër")]
        Order = 3,

        [Display(Name = "Vendim")]
        Decision = 4,

        [Display(Name = "Njoftim")]
        Notice = 5,

        [Display(Name = "Kërkesë")]
        Request = 6,

        [Display(Name = "Proces-Verbal")]
        Minutes = 7,

        [Display(Name = "Relacione")]
        Briefing = 8,

        [Display(Name = "Plan")]
        Plan = 9,

        [Display(Name = "Tjetër")]
        Other = 10
    }
}