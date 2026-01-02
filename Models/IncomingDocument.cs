using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet hyrëse - Shkresa të marra nga institucione të jashtme
    /// </summary>
    public class IncomingDocument : Document
    {
        [Required(ErrorMessage = "Institucioni dërgues është i detyrueshëm")]
        [Display(Name = "Institucioni Dërgues")]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        [Display(Name = "Institucioni")]
        public virtual Institution? Institution { get; set; }

        [Required(ErrorMessage = "Emri i dërguesit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i Dërguesit")]
        public string SenderName { get; set; }

        [StringLength(100)]
        [Display(Name = "Pozicioni i Dërguesit")]
        public string? SenderPosition { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email-i nuk është valid")]
        [Display(Name = "Email i Dërguesit")]
        public string? SenderEmail { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Numri i telefonit nuk është valid")]
        [Display(Name = "Telefoni i Dërguesit")]
        public string? SenderPhone { get; set; }

        [Required(ErrorMessage = "Data e marrjes është e detyrueshme")]
        [Display(Name = "Data e Marrjes")]
        [DataType(DataType.Date)]
        public DateTime ReceivedDate { get; set; } = DateTime.Now.Date;

        [Display(Name = "Ora e Marrjes")]
        [DataType(DataType.Time)]
        public TimeSpan ReceivedTime { get; set; } = DateTime.Now.TimeOfDay;

        [StringLength(450)]
        [Display(Name = "Marrë nga")]
        public string? ReceivedBy { get; set; }

        [ForeignKey("ReceivedBy")]
        public virtual ApplicationUser? Receiver { get; set; }

        [Required(ErrorMessage = "Mënyra e dërgesës është e detyrueshme")]
        [Display(Name = "Mënyra e Dërgesës")]
        public DeliveryMethod DeliveryMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Numri Origjinal i Dokumentit")]
        public string? OriginalDocumentNumber { get; set; }

        [Display(Name = "Data Origjinale e Dokumentit")]
        [DataType(DataType.Date)]
        public DateTime? OriginalDocumentDate { get; set; }

        [Display(Name = "Kërkon Përgjigje")]
        public bool RequiresResponse { get; set; } = false;

        [Display(Name = "Data e Kthimit të Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDeadline { get; set; }

        [Display(Name = "Është Përgjigjur")]
        public bool IsResponded { get; set; } = false;

        [Display(Name = "Data e Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDate { get; set; }

        [Display(Name = "Numri i Protokollit të Përgjigjes")]
        public int? ResponseDocumentId { get; set; }

        [ForeignKey("ResponseDocumentId")]
        public virtual OutgoingDocument? ResponseDocument { get; set; }

        [Display(Name = "Ka Kopje të Fizikshme")]
        public bool HasPhysicalCopy { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Vendndodhja Fizike")]
        public string? PhysicalLocation { get; set; }

        [Display(Name = "Numri i Zarf/Pako")]
        public string? EnvelopeNumber { get; set; }

        [Display(Name = "Ka Vulosje")]
        public bool HasSeal { get; set; } = false;

        [Display(Name = "Është Konfidencial")]
        public bool IsConfidential { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Vërejtje për Dërgesën")]
        [DataType(DataType.MultilineText)]
        public string? DeliveryNotes { get; set; }

        [Display(Name = "Është në Origjinal")]
        public bool IsOriginal { get; set; } = true;

        [StringLength(450)]
        [Display(Name = "Caktuar Për")]
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        [Display(Name = "Data e Caktimit")]
        public DateTime? AssignedDate { get; set; }

        public IncomingDocument()
        {
            DocumentType = DocumentType.Incoming;
        }
    }

    /// <summary>
    /// Mënyrat e dërgesës së dokumenteve
    /// </summary>
    public enum DeliveryMethod
    {
        [Display(Name = "Postë")]
        Mail = 1,

        [Display(Name = "Email")]
        Email = 2,

        [Display(Name = "Dorë për Dorë")]
        HandDelivery = 3,

        [Display(Name = "Faks")]
        Fax = 4,

        [Display(Name = "Kurier")]
        Courier = 5,

        [Display(Name = "Portal Elektronik")]
        ElectronicPortal = 6,

        [Display(Name = "Tjetër")]
        Other = 7
    }
}