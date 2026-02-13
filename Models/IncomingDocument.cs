using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class IncomingDocument : Document
    {
        [Required(ErrorMessage = "Institucioni dërgues është i detyrueshëm")]
        [Display(Name = "Institucioni Dërgues")]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }

        [Required(ErrorMessage = "Emri i dërguesit është i detyrueshëm")]
        [StringLength(200)]
        [Display(Name = "Emri i Dërguesit")]
        public string SenderName { get; set; }

        [Required(ErrorMessage = "Data e marrjes është e detyrueshme")]
        [Display(Name = "Data e Marrjes")]
        [DataType(DataType.Date)]
        public DateTime ReceivedDate { get; set; } = DateTime.Now.Date;

        [Display(Name = "Marrë nga")]
        public int? ReceivedBy { get; set; }

        [ForeignKey("ReceivedBy")]
        public virtual Users? Receiver { get; set; }

        [StringLength(100)]
        [Display(Name = "Numri Origjinal i Dokumentit")]
        public string? OriginalDocumentNumber { get; set; }

        [Display(Name = "Data Origjinale e Dokumentit")]
        [DataType(DataType.Date)]
        public DateTime? OriginalDocumentDate { get; set; }

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