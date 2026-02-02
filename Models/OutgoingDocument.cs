using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    /// <summary>
    /// Dokumentet dalëse - Shkresa të dërguara në institucione të jashtme
    /// </summary>
    public class OutgoingDocument : Document
    {
        // === INFORMACIONI I MARRËSIT ===
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
        [EmailAddress(ErrorMessage = "Email-i nuk është valid")]
        [Display(Name = "Email i Marrësit")]
        public string? RecipientEmail { get; set; }

        // === MËNYRA E DËRGESËS ===
        [Display(Name = "Mënyra e Dërgesës")]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Email;

        // === LIDHJA ME DOKUMENTIN HYRËS ===
        [Display(Name = "Është Përgjigje")]
        public bool IsResponse { get; set; } = false;

        [Display(Name = "Dokumeni Origjinal (Hyrës)")]
        public int? OriginalIncomingDocumentId { get; set; }

        [ForeignKey("OriginalIncomingDocumentId")]
        public virtual IncomingDocument? OriginalIncomingDocument { get; set; }

        // === ARKIVIMI ===
        [Display(Name = "Ka Kopje për Arkiv")]
        public bool HasArchiveCopy { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Vendndodhja e Kopjes në Arkiv")]
        public string? ArchiveLocation { get; set; }

        public OutgoingDocument()
        {
            DocumentType = DocumentType.Outgoing;
        }
    }
}
