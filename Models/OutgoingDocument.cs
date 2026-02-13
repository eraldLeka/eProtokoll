using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class OutgoingDocument : Document
    {
        [Required(ErrorMessage = "Institucioni marrës është i detyrueshëm")]
        [Display(Name = "Institucioni Marrës")]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }

        [Required(ErrorMessage = "Emri i marrësit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i Marrësit")]
        public string RecipientName { get; set; }

        [Display(Name = "Është Përgjigje")]
        public bool IsResponse { get; set; } = false;

        [Display(Name = "Dokumeni Origjinal (Hyrës)")]
        public int? OriginalIncomingDocumentId { get; set; }

        [ForeignKey("OriginalIncomingDocumentId")]
        public virtual IncomingDocument? OriginalIncomingDocument { get; set; }

        [StringLength(100)]
        [Display(Name = "Vendndodhja e Kopjes në Arkiv")]
        public string? ArchiveLocation { get; set; }

        public OutgoingDocument()
        {
            DocumentType = DocumentType.Outgoing;
        }
    }
}