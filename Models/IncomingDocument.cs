using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet hyrëse - Shkresa të marra nga institucione të jashtme
    /// </summary>
    public class IncomingDocument : Document
    {
        // === INFORMACIONI I DËRGUESIT ===

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
        [EmailAddress(ErrorMessage = "Email-i nuk është valid")]
        [Display(Name = "Email i Dërguesit")]
        public string? SenderEmail { get; set; }


        // === MARRJA E DOKUMENTIT ===

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

        [Display(Name = "Mënyra e Dërgesës")]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Email;

        // === DOKUMENTI ORIGJINAL (nga institucioni tjetër) ===

        [StringLength(100)]
        [Display(Name = "Numri Origjinal i Dokumentit")]
        public string? OriginalDocumentNumber { get; set; }

        [Display(Name = "Data Origjinale e Dokumentit")]
        [DataType(DataType.Date)]
        public DateTime? OriginalDocumentDate { get; set; }

        // === PËRGJIGJA ===

        [Display(Name = "Data e Afatit për Përgjigje")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDeadline { get; set; }

        [Display(Name = "Është Përgjigjur")]
        public bool IsResponded { get; set; } = false;

        [Display(Name = "Data e Përgjigjes")]
        [DataType(DataType.Date)]
        public DateTime? ResponseDate { get; set; }

        [Display(Name = "Dokumenti i Përgjigjes")]
        public int? ResponseDocumentId { get; set; }

        [ForeignKey("ResponseDocumentId")]
        public virtual OutgoingDocument? ResponseDocument { get; set; }

        public IncomingDocument()
        {
            DocumentType = DocumentType.Incoming;
        }
    }
}