using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eProtokoll.Models
{
    public class InternalDocument : Document
    {
        [StringLength(100)]
        [Display(Name = "Departamenti Dërgues")]
        public string? FromDepartment { get; set; }

        [StringLength(100)]
        [Display(Name = "Departamenti Marrës")]
        public string? ToDepartment { get; set; }

        [NotMapped]
        public bool IsResponse { get; set; }

        [Display(Name = "Dokumenti Origjinal (Brendshëm)")]
        public int? OriginalInternalDocumentId { get; set; }

        [ForeignKey("OriginalInternalDocumentId")]
        public virtual InternalDocument? OriginalInternalDocument { get; set; }

        public int? ResponseDocumentId { get; set; }

        [ForeignKey("ResponseDocumentId")]
        public virtual InternalDocument? ResponseDocument { get; set; }

        public DateTime? ResponseDate { get; set; }

        public InternalDocument()
        {
            DocumentType = DocumentType.Internal;
        }
    }
}