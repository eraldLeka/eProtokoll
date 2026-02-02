using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    /// <summary>
    /// Llojet e dokumenteve
    /// </summary>
    public enum DocumentType
    {
        [Display(Name = "Dokument Hyrës")]
        Incoming = 1,

        [Display(Name = "Dokument Dalës")]
        Outgoing = 2,

        [Display(Name = "Dokument i Brendshëm")]
        Internal = 3
    }

    /// <summary>
    /// Statuset e dokumenteve
    /// </summary>
    public enum DocumentStatus
    {
        [Display(Name = "I Protokolluar")]
        Registered = 1,

        [Display(Name = "Në Proces")]
        InProgress = 2,

        [Display(Name = "I Përfunduar")]
        Completed = 3
    }

    /// <summary>
    /// Prioriteti i dokumenteve
    /// </summary>
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

    /// <summary>
    /// Mënyrat e dërgesës së dokumenteve
    /// </summary>
    public enum DeliveryMethod
    {
        [Display(Name = "Email")]
        Email = 1,

        [Display(Name = "Dorazi")]
        HandDelivery = 2,

        [Display(Name = "Postë")]
        Mail = 3,

        [Display(Name = "Kurier")]
        Courier = 4,

        [Display(Name = "Faks")]
        Fax = 5,

        [Display(Name = "Portal Elektronik")]
        ElectronicPortal = 6,

        [Display(Name = "Tjetër")]
        Other = 7
    }
}