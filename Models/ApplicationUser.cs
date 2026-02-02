using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ===================== BASIC INFO =====================

        [Required(ErrorMessage = "Emri është i detyrueshëm")]
        [StringLength(50, ErrorMessage = "Emri nuk mund të jetë më shumë se 50 karaktere")]
        [Display(Name = "Emri")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Mbiemri është i detyrueshëm")]
        [StringLength(50, ErrorMessage = "Mbiemri nuk mund të jetë më shumë se 50 karaktere")]
        [Display(Name = "Mbiemri")]
        public string LastName { get; set; } = null!;

        [Display(Name = "Emri i plotë")]
        public string FullName => $"{FirstName} {LastName}";

        // ===================== WORK INFO =====================

        [StringLength(100)]
        [Display(Name = "Pozicioni")]
        public string? Position { get; set; }

        [StringLength(100)]
        [Display(Name = "Departmenti")]
        public string? Department { get; set; }

        // ===================== BUSINESS ROLE =====================
        // ❗ Vetëm për logjikë biznesi / UI
        // ❌ NUK përdoret për [Authorize]

        [Required]
        [Display(Name = "Roli")]
        public UserRole Role { get; set; }

        // ===================== STATUS & AUDIT =====================

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data e krijimit")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Data e modifikimit")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(36)]
        [Display(Name = "Modifikuar nga")]
        public string? ModifiedBy { get; set; }

        // ===================== NAVIGATION PROPERTIES =====================

        [Display(Name = "Dokumentet e krijuara")]
        public virtual ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();

        [Display(Name = "Dokumentet e caktuara")]
        public virtual ICollection<DocumentTracking> AssignedDocuments { get; set; } = new List<DocumentTracking>();

        [Display(Name = "Afatet")]
        public virtual ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();

        // ===================== ENUM =====================

        public enum UserRole
        {
            [Display(Name = "Administrator")]
            Administrator = 1,

            [Display(Name = "Menaxher")]
            Manager = 2,

            [Display(Name = "Punonjës")]
            Employee = 3
        }
    }
}
