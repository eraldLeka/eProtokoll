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

        [Display(Name = "I Përfunduar")]
        Completed = 3
    }

    public enum Priority
    {
        [Display(Name = "I Ulët")]
        Low = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "I Lartë")]
        High = 3,

        [Display(Name = "Urgjent")]
        Urgent = 4
    }
}