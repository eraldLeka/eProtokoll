using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace eProtokoll.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Emri është i detyrueshëm")]
        [StringLength(50, ErrorMessage = "Emri nuk mund të jetë më shumë se 50 karaktere")]
        [Display(Name = "Emri")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Mbiemri është i detyrueshëm")]
        [StringLength(50, ErrorMessage = "Mbiemri nuk mund të jetë më shumë se 50 karaktere")]
        [Display(Name = "Mbiemri")]
        public string LastName { get; set; }

        [Display(Name = "Emri i plote")]
        public string FullName => $"{FirstName} {LastName}";
        [StringLength(100)]
        [Display(Name = "Pozicioni")]
        public string? Position { get; set; }

        [StringLength(100)]
        [Display(Name = "Departmenti")]
        public string? Department { get; set; }

        [Required]
        [Display(Name = "Roli")]
        public UserRole Role { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data e krijimit")]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = "Data e modifikimit")]
        public DateTime? ModifiedData { get; set; }

        [StringLength(36)]
        [Display(Name = "Modifikuar nga ")]
        public string? ModifiedBy { get; set; }

        //NAVIGATION PROPERTIES
        [Display(Name = "Dokumentet e krijuara")]
        public virtual ICollection<Document>? CreatedDocuments { get; set; }

        [Display(Name ="Dokumentet e caktuara")]
        public virtual ICollection<DocumentTracking>? AssignedDocuments {  get; set; }

        [Display(Name = "Afatet")]
        public virtual ICollection<Deadline>? Deadlines { get; set; }


        //Nivelet e perdoruesit ne sistem
        public enum UserRole
        {
            [Display(Name = "Administrator")]
            Administrator = 1,

            [Display(Name ="Menaxher")]
            Manager = 2,

            [Display(Name ="Punonjes")]
            Employee = 3

        }

          
        
    }
}
