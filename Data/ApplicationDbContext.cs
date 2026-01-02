using eProtokoll.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace eProtokoll.Data
{
    /// <summary>
    /// Konteksti i databazës për aplikacionin eProtokoll
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet për të gjitha modelet
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<Classification> Classifications { get; set; }
        public DbSet<ProtocolSettings> ProtocolSettings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<IncomingDocument> IncomingDocuments { get; set; }
        public DbSet<OutgoingDocument> OutgoingDocuments { get; set; }
        public DbSet<InternalDocument> InternalDocuments { get; set; }
        public DbSet<DocumentTracking> DocumentTrackings { get; set; }
        public DbSet<Deadline> Deadlines { get; set; }
        public DbSet<DocumentAttachment> DocumentAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // KONFIGURIMI I MODELEVE
            // ============================================================

            // ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();

                // Relacioni me dokumentet e krijuara
                entity.HasMany(u => u.CreatedDocuments)
                    .WithOne(d => d.Creator)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me gjurmimin
                entity.HasMany(u => u.AssignedDocuments)
                    .WithOne(t => t.AssignedToUser)
                    .HasForeignKey(t => t.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me afatet
                entity.HasMany(u => u.Deadlines)
                    .WithOne(d => d.ResponsibleUser)
                    .HasForeignKey(d => d.ResponsibleUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Institution
            modelBuilder.Entity<Institution>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.TaxCode).IsUnique();

                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Type).IsRequired();
            });

            // Classification
            modelBuilder.Entity<Classification>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Level);

                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Level).IsRequired();
            });

            // ProtocolSettings
            modelBuilder.Entity<ProtocolSettings>(entity =>
            {
                entity.HasIndex(e => e.Year).IsUnique();

                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.IncomingCurrentNumber).IsRequired();
                entity.Property(e => e.OutgoingCurrentNumber).IsRequired();
                entity.Property(e => e.InternalCurrentNumber).IsRequired();
            });

            // Document (Base Class)
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasIndex(e => e.ProtocolNumber).IsUnique();
                entity.HasIndex(e => e.ProtocolDate);
                entity.HasIndex(e => e.DocumentType);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.ProtocolNumber).IsRequired();
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);

                

                // Relacioni me Classification
                entity.HasOne(d => d.Classification)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.ClassificationId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me Attachments
                entity.HasMany(d => d.Attachments)
                    .WithOne(a => a.Document)
                    .HasForeignKey(a => a.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacioni me Trackings
                entity.HasMany(d => d.Trackings)
                    .WithOne(t => t.Document)
                    .HasForeignKey(t => t.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacioni me Deadlines
                entity.HasMany(d => d.Deadlines)
                    .WithOne(dl => dl.Document)
                    .HasForeignKey(dl => dl.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // IncomingDocument
            modelBuilder.Entity<IncomingDocument>(entity =>
            {
                entity.HasIndex(e => e.InstitutionId);
                entity.HasIndex(e => e.ReceivedDate);

                // Relacioni me Institution
                entity.HasOne(d => d.Institution)
                    .WithMany(i => i.IncomingDocuments)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me ResponseDocument (OutgoingDocument)
                entity.HasOne(d => d.ResponseDocument)
                    .WithOne(o => o.OriginalIncomingDocument)
                    .HasForeignKey<OutgoingDocument>(o => o.OriginalIncomingDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OutgoingDocument
            modelBuilder.Entity<OutgoingDocument>(entity =>
            {
                entity.HasIndex(e => e.InstitutionId);
                entity.HasIndex(e => e.SentDate);

                // Relacioni me Institution
                entity.HasOne(d => d.Institution)
                    .WithMany(i => i.OutgoingDocuments)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ShipmentCost)
                    .HasColumnType("decimal(18,2)");
            });

            // InternalDocument
            modelBuilder.Entity<InternalDocument>(entity =>
            {
                entity.HasIndex(e => e.FromDepartment);
                entity.HasIndex(e => e.ToDepartment);

                // Self-referencing për dokumentet që përgjigjen njëri-tjetrin
                entity.HasOne(d => d.ResponseDocument)
                    .WithMany()
                    .HasForeignKey(d => d.ResponseDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.RelatedDocument)
                    .WithMany()
                    .HasForeignKey(d => d.RelatedDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DocumentTracking
            modelBuilder.Entity<DocumentTracking>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.AssignedToUserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.DueDate);

                // Self-referencing për delegimet
                entity.HasOne(t => t.ParentTracking)
                    .WithMany(t => t.SubDelegations)
                    .HasForeignKey(t => t.ParentTrackingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.DelegatedToTracking)
                    .WithMany()
                    .HasForeignKey(t => t.DelegatedToTrackingId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me AssignedByUser
                entity.HasOne(t => t.AssignedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Deadline
            modelBuilder.Entity<Deadline>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ResponsibleUserId);

                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);

                // Relacioni me CompletedByUser
                entity.HasOne(d => d.CompletedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.CompletedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me EscalateToUser
                entity.HasOne(d => d.EscalateToUser)
                    .WithMany()
                    .HasForeignKey(d => d.EscalateToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me Creator
                entity.HasOne(d => d.Creator)
                    .WithMany()
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DocumentAttachment
            modelBuilder.Entity<DocumentAttachment>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.UploadedDate);
                entity.HasIndex(e => e.FileHash);

                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileSize).IsRequired();

                // Self-referencing për versionet
                entity.HasOne(a => a.PreviousVersion)
                    .WithMany(a => a.Versions)
                    .HasForeignKey(a => a.PreviousVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacioni me Uploader
                entity.HasOne(a => a.Uploader)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // SEED DATA (TË DHËNA FILLESTARE)
            // ============================================================

            // Seed Classifications
            modelBuilder.Entity<Classification>().HasData(
                new Classification
                {
                    ClassificationId = 1,
                    Name = "Publik",
                    Level = AccessLevel.Public,
                    Description = "Dokumente publike që mund të shihen nga të gjithë",
                    RetentionYears = 5,
                    RequiresApproval = false,
                    AllowPrint = true,
                    AllowDownload = true,
                    AllowCopy = true,
                    ColorCode = "#28a745",
                    SortOrder = 1,
                    IsActive = true,
                    IsDefault = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 2,
                    Name = "I Brendshëm",
                    Level = AccessLevel.Internal,
                    Description = "Vetëm për punonjësit e autorizuar",
                    RetentionYears = 10,
                    RequiresApproval = false,
                    AllowPrint = true,
                    AllowDownload = true,
                    AllowCopy = true,
                    ColorCode = "#17a2b8",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 3,
                    Name = "Konfidencial",
                    Level = AccessLevel.Confidential,
                    Description = "Vetëm disa punonjës të caktuar",
                    RetentionYears = 15,
                    RequiresApproval = true,
                    AllowPrint = false,
                    AllowDownload = false,
                    AllowCopy = false,
                    ColorCode = "#ffc107",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 4,
                    Name = "Sekret",
                    Level = AccessLevel.Secret,
                    Description = "Vetëm menaxherët dhe administratorët",
                    RetentionYears = 20,
                    RequiresApproval = true,
                    AllowPrint = false,
                    AllowDownload = false,
                    AllowCopy = false,
                    ColorCode = "#fd7e14",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 5,
                    Name = "Tepër Sekret",
                    Level = AccessLevel.TopSecret,
                    Description = "Vetëm administratorët",
                    RetentionYears = 30,
                    RequiresApproval = true,
                    AllowPrint = false,
                    AllowDownload = false,
                    AllowCopy = false,
                    ColorCode = "#dc3545",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed ProtocolSettings për vitin aktual
            modelBuilder.Entity<ProtocolSettings>().HasData(
                new ProtocolSettings
                {
                    ProtocolSettingsId = 1,
                    Year = DateTime.Now.Year,
                    IncomingStartNumber = 1,
                    IncomingCurrentNumber = 1,
                    IncomingPrefix = "H",
                    OutgoingStartNumber = 1,
                    OutgoingCurrentNumber = 1,
                    OutgoingPrefix = "D",
                    InternalStartNumber = 1,
                    InternalCurrentNumber = 1,
                    InternalPrefix = "B",
                    ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                    NumberPadding = 4,
                    AutoResetYearly = true,
                    AllowManualEdit = false,
                    ShowYearInNumber = true,
                    UseSeparatorSlash = true,
                    IsActive = true,
                    IsClosed = false,
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}