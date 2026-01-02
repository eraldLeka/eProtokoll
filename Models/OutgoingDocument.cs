using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet dalëse - Shkresa të dërguara në institucione të jashtme
    /// </summary>
    public class OutgoingDocument : Document
    {
        [Required(ErrorMessage = "Institucioni marrës është i detyrueshëm")]
        [Display(Name = "Institucioni Marrës")]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        [Display(Name = "Institucioni")]
        public virtual Institution? Institution { get; set; }

        [Required(ErrorMessage = "Emri i marrësit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i Marrësit")]
        public string RecipientName { get; set; }

        [StringLength(100)]
        [Display(Name = "Pozicioni i Marrësit")]
        public string? RecipientPosition { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email-i nuk është valid")]
        [Display(Name = "Email i Marrësit")]
        public string? RecipientEmail { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Numri i telefonit nuk është valid")]
        [Display(Name = "Telefoni i Marrësit")]
        public string? RecipientPhone { get; set; }

        [StringLength(500)]
        [Display(Name = "Adresa e Plotë e Marrësit")]
        public string? RecipientAddress { get; set; }

        [Display(Name = "Data e Dërgimit")]
        [DataType(DataType.Date)]
        public DateTime? SentDate { get; set; }

        [Display(Name = "Ora e Dërgimit")]
        [DataType(DataType.Time)]
        public TimeSpan? SentTime { get; set; }

        [StringLength(450)]
        [Display(Name = "Dërguar nga")]
        public string? SentBy { get; set; }

        [ForeignKey("SentBy")]
        public virtual ApplicationUser? Sender { get; set; }

        [Required(ErrorMessage = "Mënyra e dërgesës është e detyrueshme")]
        [Display(Name = "Mënyra e Dërgesës")]
        public DeliveryMethod DeliveryMethod { get; set; }

        [Display(Name = "Është Përgjigje")]
        public bool IsResponse { get; set; } = false;

        [Display(Name = "Dokumeni Origjinal (Hyrës)")]
        public int? OriginalIncomingDocumentId { get; set; }

        [ForeignKey("OriginalIncomingDocumentId")]
        public virtual IncomingDocument? OriginalIncomingDocument { get; set; }

        [Required(ErrorMessage = "Firmëtari është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Firmëtari")]
        public string SignedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Pozicioni i Firmëtarit")]
        public string? SignerPosition { get; set; }

        [Display(Name = "Data e Firmosjes")]
        [DataType(DataType.Date)]
        public DateTime? SignedDate { get; set; }

        [Display(Name = "Ka Firmë Digjitale")]
        public bool HasDigitalSignature { get; set; } = false;

        [Display(Name = "Është i Vulosur")]
        public bool IsSealed { get; set; } = false;

        [Display(Name = "Numri i Kopjeve të Dërguara")]
        public int NumberOfCopies { get; set; } = 1;

        [Display(Name = "Kërkon Konfirmim Marrjeje")]
        public bool RequiresDeliveryConfirmation { get; set; } = false;

        [Display(Name = "Është Konfirmuar Marrja")]
        public bool IsDeliveryConfirmed { get; set; } = false;

        [Display(Name = "Data e Konfirmimit")]
        [DataType(DataType.Date)]
        public DateTime? ConfirmationDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Konfirmuar nga")]
        public string? ConfirmedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Numri i Gjurmimit (Tracking)")]
        public string? TrackingNumber { get; set; }

        [Display(Name = "Statusi i Dërgesës")]
        public ShipmentStatus ShipmentStatus { get; set; } = ShipmentStatus.Prepared;

        [StringLength(500)]
        [Display(Name = "Vërejtje për Dërgesën")]
        [DataType(DataType.MultilineText)]
        public string? ShipmentNotes { get; set; }

        [Display(Name = "Kosto e Dërgesës")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShipmentCost { get; set; }

        [StringLength(100)]
        [Display(Name = "Kompania e Dërgesës")]
        public string? ShipmentCompany { get; set; }

        [Display(Name = "Ka Kopje për Arkiv")]
        public bool HasArchiveCopy { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Vendndodhja e Kopjes")]
        public string? ArchiveLocation { get; set; }

        [StringLength(1000)]
        [Display(Name = "Lista e Kopjeve Shtesë")]
        public string? CarbonCopyList { get; set; }

        [StringLength(450)]
        [Display(Name = "Përgatitur nga")]
        public string? PreparedBy { get; set; }

        [ForeignKey("PreparedBy")]
        public virtual ApplicationUser? Preparer { get; set; }

        [Display(Name = "Data e Përgatitjes")]
        public DateTime? PreparedDate { get; set; }

        public OutgoingDocument()
        {
            DocumentType = DocumentType.Outgoing;
        }
    }

    /// <summary>
    /// Statusi i dërgesës së dokumentit
    /// </summary>
    public enum ShipmentStatus
    {
        [Display(Name = "Në Përgatitje")]
        Prepared = 1,

        [Display(Name = "Gati për Dërgim")]
        ReadyToSend = 2,

        [Display(Name = "Në Dërgim")]
        InTransit = 3,

        [Display(Name = "Dorëzuar")]
        Delivered = 4,

        [Display(Name = "Dështoi")]
        Failed = 5,

        [Display(Name = "Kthyer Mbrapsht")]
        Returned = 6
    }
}