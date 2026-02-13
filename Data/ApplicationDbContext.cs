using eProtokoll.Models;
using Microsoft.EntityFrameworkCore;

namespace eProtokoll.Data
{
   
    public class ApplicationDbContext : DbContext
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

           
            // ApplicationUser
            modelBuilder.Entity<Users>(entity =>
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
                    .HasForeignKey<IncomingDocument>(i => i.ResponseDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OutgoingDocument
            modelBuilder.Entity<OutgoingDocument>(entity =>
            {
                entity.HasIndex(e => e.InstitutionId);

                // Relacioni me Institution
                entity.HasOne(d => d.Institution)
                    .WithMany(i => i.OutgoingDocuments)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // InternalDocument
            modelBuilder.Entity<InternalDocument>(entity =>
            {
                entity.HasIndex(e => e.FromDepartment);
                entity.HasIndex(e => e.ToDepartment);
            });

            // DocumentTracking
            modelBuilder.Entity<DocumentTracking>(entity =>
            {
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.AssignedToUserId);
                entity.HasIndex(e => e.DueDate);

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
                entity.HasIndex(e => e.ResponsibleUserId);
                entity.HasIndex(e => e.IsCompleted); 

                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);

                // Relacioni me CompletedByUser
                entity.HasOne(d => d.CompletedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.CompletedBy)
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

                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileSize).IsRequired();

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
                    ColorCode = "#28a745",
                    SortOrder = 1,
                    IsActive = true,
                    IsDefault = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 2,
                    Name = "I Kufizuar",
                    Level = AccessLevel.Restricted,
                    Description = "Vetëm për punonjësit e përzgjedhur (assigned)",
                    RetentionYears = 10,
                    ColorCode = "#ffc107",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Classification
                {
                    ClassificationId = 3,
                    Name = "Sekret",
                    Level = AccessLevel.Secret,
                    Description = "Vetëm menaxherët dhe administratorët",
                    RetentionYears = 20,
                    ColorCode = "#dc3545",
                    SortOrder = 3,
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
            modelBuilder.Entity<Document>()
    .HasDiscriminator<string>("Discriminator")
    .HasValue<Document>("Document")                     
    .HasValue<IncomingDocument>("IncomingDocument")     
    .HasValue<OutgoingDocument>("OutgoingDocument")     
    .HasValue<InternalDocument>("InternalDocument");
        }
    }
}