using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    public enum DocumentType
    {
        [Display(Name = "Dokument Hyrës")]
        Incoming = 1,

        [Display(Name = "Dokument Dalës")]
        Outgoing = 2,

        [Display(Name = "Dokument i Brendshëm")]
        Internal = 3
    }

    public enum DocumentStatus
    {
        [Display(Name = "I Protokolluar")]
        Registered = 1,

        [Display(Name = "Në Proces")]
        InProgress = 2,

        [Display(Name = "I Mbyllur")]
        Closed = 3
    }

    public enum Priority
    {
        [Display(Name = "I Ulët")]
        Low = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "I Lartë")]
        High = 3,

    }
    public enum Classification
    {
        [Display(Name = "Publik")]
        Public = 1,
        [Display(Name = "I kufizuar")]
        Confidential = 2,
        [Display(Name = "Sekret")]
        Secret = 3  
    }


 public enum FileCategory
{
    [Display(Name = "PDF")]
    PDF = 1,

    [Display(Name = "I Skanuar")]
    Scanned = 2
}
}