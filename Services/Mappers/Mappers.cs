using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using static eProtokoll.Models.Institution;

namespace eProtokoll.Services.Mappers
{
    // ==================== DOCUMENT MAPPERS ====================

    public static class DocumentMapper
    {
        public static Document MapToDocument(SqlDataReader reader)
        {
            return new Document
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                ReferenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                ReferenceDate = reader.IsDBNull(reader.GetOrdinal("ReferenceDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReferenceDate")),
                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                HasDeadline = reader.GetBoolean(reader.GetOrdinal("HasDeadline")),
                DeadlineDate = reader.IsDBNull(reader.GetOrdinal("DeadlineDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DeadlineDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                PageCount = reader.IsDBNull(reader.GetOrdinal("PageCount")) ? null : reader.GetInt32(reader.GetOrdinal("PageCount")),
                Language = reader.IsDBNull(reader.GetOrdinal("Language")) ? null : reader.GetString(reader.GetOrdinal("Language")),
                IsScanned = reader.GetBoolean(reader.GetOrdinal("IsScanned")),
                HasAttachments = reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                ArchivedDate = reader.IsDBNull(reader.GetOrdinal("ArchivedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ArchivedDate")),
                ArchivedBy = reader.IsDBNull(reader.GetOrdinal("ArchivedBy")) ? null : reader.GetString(reader.GetOrdinal("ArchivedBy")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
            };
        }

        public static IncomingDocument MapToIncomingDocument(SqlDataReader reader)
        {
            return new IncomingDocument
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                SenderPosition = reader.IsDBNull(reader.GetOrdinal("SenderPosition")) ? null : reader.GetString(reader.GetOrdinal("SenderPosition")),
                SenderEmail = reader.IsDBNull(reader.GetOrdinal("SenderEmail")) ? null : reader.GetString(reader.GetOrdinal("SenderEmail")),
                SenderPhone = reader.IsDBNull(reader.GetOrdinal("SenderPhone")) ? null : reader.GetString(reader.GetOrdinal("SenderPhone")),
                ReceivedDate = reader.GetDateTime(reader.GetOrdinal("ReceivedDate")),
                ReceivedTime = reader.GetTimeSpan(reader.GetOrdinal("ReceivedTime")),
                ReceivedBy = reader.IsDBNull(reader.GetOrdinal("ReceivedBy")) ? null : reader.GetString(reader.GetOrdinal("ReceivedBy")),
                DeliveryMethod = (DeliveryMethod)reader.GetInt32(reader.GetOrdinal("DeliveryMethod")),
                OriginalDocumentNumber = reader.IsDBNull(reader.GetOrdinal("OriginalDocumentNumber")) ? null : reader.GetString(reader.GetOrdinal("OriginalDocumentNumber")),
                OriginalDocumentDate = reader.IsDBNull(reader.GetOrdinal("OriginalDocumentDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OriginalDocumentDate")),
                RequiresResponse = reader.GetBoolean(reader.GetOrdinal("RequiresResponse")),
                ResponseDeadline = reader.IsDBNull(reader.GetOrdinal("ResponseDeadline")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDeadline")),
                IsResponded = reader.GetBoolean(reader.GetOrdinal("IsResponded")),
                ResponseDate = reader.IsDBNull(reader.GetOrdinal("ResponseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDate")),
                ResponseDocumentId = reader.IsDBNull(reader.GetOrdinal("ResponseDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("ResponseDocumentId")),
                HasPhysicalCopy = reader.GetBoolean(reader.GetOrdinal("HasPhysicalCopy")),
                PhysicalLocation = reader.IsDBNull(reader.GetOrdinal("PhysicalLocation")) ? null : reader.GetString(reader.GetOrdinal("PhysicalLocation")),
                EnvelopeNumber = reader.IsDBNull(reader.GetOrdinal("EnvelopeNumber")) ? null : reader.GetString(reader.GetOrdinal("EnvelopeNumber")),
                HasSeal = reader.GetBoolean(reader.GetOrdinal("HasSeal")),
                IsConfidential = reader.GetBoolean(reader.GetOrdinal("IsConfidential")),
                DeliveryNotes = reader.IsDBNull(reader.GetOrdinal("DeliveryNotes")) ? null : reader.GetString(reader.GetOrdinal("DeliveryNotes")),
                IsOriginal = reader.GetBoolean(reader.GetOrdinal("IsOriginal")),
                AssignedTo = reader.IsDBNull(reader.GetOrdinal("AssignedTo")) ? null : reader.GetString(reader.GetOrdinal("AssignedTo")),
                AssignedDate = reader.IsDBNull(reader.GetOrdinal("AssignedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("AssignedDate"))
            };
        }

        public static OutgoingDocument MapToOutgoingDocument(SqlDataReader reader)
        {
            return new OutgoingDocument
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                RecipientName = reader.GetString(reader.GetOrdinal("RecipientName")),
                RecipientPosition = reader.IsDBNull(reader.GetOrdinal("RecipientPosition")) ? null : reader.GetString(reader.GetOrdinal("RecipientPosition")),
                RecipientEmail = reader.IsDBNull(reader.GetOrdinal("RecipientEmail")) ? null : reader.GetString(reader.GetOrdinal("RecipientEmail")),
                RecipientPhone = reader.IsDBNull(reader.GetOrdinal("RecipientPhone")) ? null : reader.GetString(reader.GetOrdinal("RecipientPhone")),
                RecipientAddress = reader.IsDBNull(reader.GetOrdinal("RecipientAddress")) ? null : reader.GetString(reader.GetOrdinal("RecipientAddress")),
                SentDate = reader.IsDBNull(reader.GetOrdinal("SentDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SentDate")),
                SentTime = reader.IsDBNull(reader.GetOrdinal("SentTime")) ? null : reader.GetTimeSpan(reader.GetOrdinal("SentTime")),
                SentBy = reader.IsDBNull(reader.GetOrdinal("SentBy")) ? null : reader.GetString(reader.GetOrdinal("SentBy")),
                DeliveryMethod = (DeliveryMethod)reader.GetInt32(reader.GetOrdinal("DeliveryMethod")),
                IsResponse = reader.GetBoolean(reader.GetOrdinal("IsResponse")),
                OriginalIncomingDocumentId = reader.IsDBNull(reader.GetOrdinal("OriginalIncomingDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("OriginalIncomingDocumentId")),
                SignedBy = reader.GetString(reader.GetOrdinal("SignedBy")),
                SignerPosition = reader.IsDBNull(reader.GetOrdinal("SignerPosition")) ? null : reader.GetString(reader.GetOrdinal("SignerPosition")),
                SignedDate = reader.IsDBNull(reader.GetOrdinal("SignedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SignedDate")),
                HasDigitalSignature = reader.GetBoolean(reader.GetOrdinal("HasDigitalSignature")),
                IsSealed = reader.GetBoolean(reader.GetOrdinal("IsSealed")),
                NumberOfCopies = reader.GetInt32(reader.GetOrdinal("NumberOfCopies")),
                RequiresDeliveryConfirmation = reader.GetBoolean(reader.GetOrdinal("RequiresDeliveryConfirmation")),
                IsDeliveryConfirmed = reader.GetBoolean(reader.GetOrdinal("IsDeliveryConfirmed")),
                ConfirmationDate = reader.IsDBNull(reader.GetOrdinal("ConfirmationDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ConfirmationDate")),
                ConfirmedBy = reader.IsDBNull(reader.GetOrdinal("ConfirmedBy")) ? null : reader.GetString(reader.GetOrdinal("ConfirmedBy")),
                TrackingNumber = reader.IsDBNull(reader.GetOrdinal("TrackingNumber")) ? null : reader.GetString(reader.GetOrdinal("TrackingNumber")),
                ShipmentStatus = (ShipmentStatus)reader.GetInt32(reader.GetOrdinal("ShipmentStatus")),
                ShipmentNotes = reader.IsDBNull(reader.GetOrdinal("ShipmentNotes")) ? null : reader.GetString(reader.GetOrdinal("ShipmentNotes")),
                ShipmentCost = reader.IsDBNull(reader.GetOrdinal("ShipmentCost")) ? null : reader.GetDecimal(reader.GetOrdinal("ShipmentCost")),
                ShipmentCompany = reader.IsDBNull(reader.GetOrdinal("ShipmentCompany")) ? null : reader.GetString(reader.GetOrdinal("ShipmentCompany")),
                HasArchiveCopy = reader.GetBoolean(reader.GetOrdinal("HasArchiveCopy")),
                ArchiveLocation = reader.IsDBNull(reader.GetOrdinal("ArchiveLocation")) ? null : reader.GetString(reader.GetOrdinal("ArchiveLocation")),
                CarbonCopyList = reader.IsDBNull(reader.GetOrdinal("CarbonCopyList")) ? null : reader.GetString(reader.GetOrdinal("CarbonCopyList")),
                PreparedBy = reader.IsDBNull(reader.GetOrdinal("PreparedBy")) ? null : reader.GetString(reader.GetOrdinal("PreparedBy")),
                PreparedDate = reader.IsDBNull(reader.GetOrdinal("PreparedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PreparedDate"))
            };
        }

        public static InternalDocument MapToInternalDocument(SqlDataReader reader)
        {
            return new InternalDocument
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                ProtocolTime = reader.GetTimeSpan(reader.GetOrdinal("ProtocolTime")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                HasAttachments = reader.GetBoolean(reader.GetOrdinal("HasAttachments")),
                FromDepartment = reader.GetString(reader.GetOrdinal("FromDepartment")),
                ToDepartment = reader.GetString(reader.GetOrdinal("ToDepartment")),
                InternalType = (InternalDocumentType)reader.GetInt32(reader.GetOrdinal("InternalType")),
                FromUserId = reader.IsDBNull(reader.GetOrdinal("FromUserId")) ? null : reader.GetString(reader.GetOrdinal("FromUserId")),
                ToUserId = reader.IsDBNull(reader.GetOrdinal("ToUserId")) ? null : reader.GetString(reader.GetOrdinal("ToUserId")),
                CarbonCopyList = reader.IsDBNull(reader.GetOrdinal("CarbonCopyList")) ? null : reader.GetString(reader.GetOrdinal("CarbonCopyList")),
                RequiresResponse = reader.GetBoolean(reader.GetOrdinal("RequiresResponse")),
                ResponseDeadline = reader.IsDBNull(reader.GetOrdinal("ResponseDeadline")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDeadline")),
                IsResponded = reader.GetBoolean(reader.GetOrdinal("IsResponded")),
                ResponseDate = reader.IsDBNull(reader.GetOrdinal("ResponseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ResponseDate")),
                ResponseDocumentId = reader.IsDBNull(reader.GetOrdinal("ResponseDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("ResponseDocumentId")),
                RequiresApproval = reader.GetBoolean(reader.GetOrdinal("RequiresApproval")),
                IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved")),
                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : reader.GetString(reader.GetOrdinal("ApprovedBy")),
                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ApprovedDate")),
                ApprovalComment = reader.IsDBNull(reader.GetOrdinal("ApprovalComment")) ? null : reader.GetString(reader.GetOrdinal("ApprovalComment")),
                RequiresSignature = reader.GetBoolean(reader.GetOrdinal("RequiresSignature")),
                SignedBy = reader.IsDBNull(reader.GetOrdinal("SignedBy")) ? null : reader.GetString(reader.GetOrdinal("SignedBy")),
                SignedDate = reader.IsDBNull(reader.GetOrdinal("SignedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SignedDate")),
                HasDigitalSignature = reader.GetBoolean(reader.GetOrdinal("HasDigitalSignature")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                ReadDate = reader.IsDBNull(reader.GetOrdinal("ReadDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReadDate")),
                ReadBy = reader.IsDBNull(reader.GetOrdinal("ReadBy")) ? null : reader.GetString(reader.GetOrdinal("ReadBy")),
                RequiresAttention = reader.GetBoolean(reader.GetOrdinal("RequiresAttention")),
                IsConfidential = reader.GetBoolean(reader.GetOrdinal("IsConfidential")),
                NumberOfCopies = reader.GetInt32(reader.GetOrdinal("NumberOfCopies")),
                HasPhysicalCopy = reader.GetBoolean(reader.GetOrdinal("HasPhysicalCopy")),
                PhysicalLocation = reader.IsDBNull(reader.GetOrdinal("PhysicalLocation")) ? null : reader.GetString(reader.GetOrdinal("PhysicalLocation")),
                IsCirculation = reader.GetBoolean(reader.GetOrdinal("IsCirculation")),
                CirculationOrder = reader.IsDBNull(reader.GetOrdinal("CirculationOrder")) ? null : reader.GetInt32(reader.GetOrdinal("CirculationOrder")),
                CirculationList = reader.IsDBNull(reader.GetOrdinal("CirculationList")) ? null : reader.GetString(reader.GetOrdinal("CirculationList")),
                InternalReferenceNumber = reader.IsDBNull(reader.GetOrdinal("InternalReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("InternalReferenceNumber")),
                RelatedDocumentId = reader.IsDBNull(reader.GetOrdinal("RelatedDocumentId")) ? null : reader.GetInt32(reader.GetOrdinal("RelatedDocumentId")),
                ActionRequired = reader.IsDBNull(reader.GetOrdinal("ActionRequired")) ? null : reader.GetString(reader.GetOrdinal("ActionRequired")),
                ActionDate = reader.IsDBNull(reader.GetOrdinal("ActionDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ActionDate")),
                ActionCompleted = reader.GetBoolean(reader.GetOrdinal("ActionCompleted")),
                ActionCompletedDate = reader.IsDBNull(reader.GetOrdinal("ActionCompletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ActionCompletedDate"))
            };
        }
    }

    // ==================== ATTACHMENT MAPPER ====================

    public static class AttachmentMapper
    {
        public static DocumentAttachment MapToDocumentAttachment(SqlDataReader reader)
        {
            return new DocumentAttachment
            {
                AttachmentId = reader.GetInt32(reader.GetOrdinal("AttachmentId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                FileExtension = reader.IsDBNull(reader.GetOrdinal("FileExtension")) ? null : reader.GetString(reader.GetOrdinal("FileExtension")),
                ContentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? null : reader.GetString(reader.GetOrdinal("ContentType")),
                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                UploadedBy = reader.IsDBNull(reader.GetOrdinal("UploadedBy")) ? null : reader.GetString(reader.GetOrdinal("UploadedBy")),
                Category = (FileCategory)reader.GetInt32(reader.GetOrdinal("Category")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                IsVirusScanned = reader.GetBoolean(reader.GetOrdinal("IsVirusScanned")),
                VirusScanDate = reader.IsDBNull(reader.GetOrdinal("VirusScanDate")) ? null : reader.GetDateTime(reader.GetOrdinal("VirusScanDate")),
                IsVirusFree = reader.GetBoolean(reader.GetOrdinal("IsClean")),
                AllowDownload = reader.GetBoolean(reader.GetOrdinal("AllowDownload")),
                AllowPrint = reader.GetBoolean(reader.GetOrdinal("AllowPrint")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                IsPrimaryDocument = reader.GetBoolean(reader.GetOrdinal("IsPrimaryDocument")),
                Version = reader.GetInt32(reader.GetOrdinal("Version")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                DeletedDate = reader.IsDBNull(reader.GetOrdinal("DeletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DeletedDate")),
                DeletedBy = reader.IsDBNull(reader.GetOrdinal("DeletedBy")) ? null : reader.GetString(reader.GetOrdinal("DeletedBy"))
            };
        }
    }

    // ==================== TRACKING MAPPER ====================

    public static class TrackingMapper
    {
        public static DocumentTracking MapToDocumentTracking(SqlDataReader reader)
        {
            return new DocumentTracking
            {
                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                AssignedToUserId = reader.GetString(reader.GetOrdinal("AssignedToUserId")),
                AssignedByUserId = reader.GetString(reader.GetOrdinal("AssignedByUserId")),
                AssignedDate = reader.GetDateTime(reader.GetOrdinal("AssignedDate")),
                AssignedTime = reader.GetTimeSpan(reader.GetOrdinal("AssignedTime")),
                Status = (TrackingStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                ActionType = (ActionType)reader.GetInt32(reader.GetOrdinal("ActionType")),
                Instructions = reader.GetString(reader.GetOrdinal("Instructions")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                HasDeadline = reader.GetBoolean(reader.GetOrdinal("HasDeadline")),
                DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                RequiresResponse = reader.GetBoolean(reader.GetOrdinal("RequiresResponse")),
                RequiresReport = reader.GetBoolean(reader.GetOrdinal("RequiresReport")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                ReadDate = reader.IsDBNull(reader.GetOrdinal("ReadDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReadDate")),
                IsAccepted = reader.GetBoolean(reader.GetOrdinal("IsAccepted")),
                AcceptedDate = reader.IsDBNull(reader.GetOrdinal("AcceptedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("AcceptedDate")),
                IsInProgress = reader.GetBoolean(reader.GetOrdinal("IsInProgress")),
                StartedDate = reader.IsDBNull(reader.GetOrdinal("StartedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedDate")),
                IsCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted")),
                CompletedDate = reader.IsDBNull(reader.GetOrdinal("CompletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedDate")),
                CompletionComment = reader.IsDBNull(reader.GetOrdinal("CompletionComment")) ? null : reader.GetString(reader.GetOrdinal("CompletionComment")),
                CompletionPercentage = reader.GetInt32(reader.GetOrdinal("CompletionPercentage")),
                IsRejected = reader.GetBoolean(reader.GetOrdinal("IsRejected")),
                RejectedDate = reader.IsDBNull(reader.GetOrdinal("RejectedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("RejectedDate")),
                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader.GetString(reader.GetOrdinal("RejectionReason")),
                IsDelegated = reader.GetBoolean(reader.GetOrdinal("IsDelegated")),
                DelegatedToTrackingId = reader.IsDBNull(reader.GetOrdinal("DelegatedToTrackingId")) ? null : reader.GetInt32(reader.GetOrdinal("DelegatedToTrackingId")),
                ParentTrackingId = reader.IsDBNull(reader.GetOrdinal("ParentTrackingId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentTrackingId")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                DurationHours = reader.IsDBNull(reader.GetOrdinal("DurationHours")) ? null : reader.GetDecimal(reader.GetOrdinal("DurationHours")),
                IsOverdue = reader.GetBoolean(reader.GetOrdinal("IsOverdue")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                SequenceNumber = reader.GetInt32(reader.GetOrdinal("SequenceNumber")),
                AttachedFiles = reader.IsDBNull(reader.GetOrdinal("AttachedFiles")) ? null : reader.GetString(reader.GetOrdinal("AttachedFiles")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }

    // ==================== DEADLINE MAPPER ====================

    public static class DeadlineMapper
    {
        public static Deadline MapToDeadline(SqlDataReader reader)
        {
            return new Deadline
            {
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                DueTime = reader.IsDBNull(reader.GetOrdinal("DueTime")) ? null : reader.GetTimeSpan(reader.GetOrdinal("DueTime")),
                ResponsibleUserId = reader.IsDBNull(reader.GetOrdinal("ResponsibleUserId")) ? null : reader.GetString(reader.GetOrdinal("ResponsibleUserId")),
                ResponsibleDepartment = reader.IsDBNull(reader.GetOrdinal("ResponsibleDepartment")) ? null : reader.GetString(reader.GetOrdinal("ResponsibleDepartment")),
                StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? null : reader.GetDateTime(reader.GetOrdinal("StartDate")),
                CompletedDate = reader.IsDBNull(reader.GetOrdinal("CompletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedDate")),
                CompletedBy = reader.IsDBNull(reader.GetOrdinal("CompletedBy")) ? null : reader.GetString(reader.GetOrdinal("CompletedBy")),
                CompletionNotes = reader.IsDBNull(reader.GetOrdinal("CompletionNotes")) ? null : reader.GetString(reader.GetOrdinal("CompletionNotes")),
                NotificationSentDate = reader.IsDBNull(reader.GetOrdinal("NotificationSentDate")) ? null : reader.GetDateTime(reader.GetOrdinal("NotificationSentDate")),
                LastReminderDate = reader.IsDBNull(reader.GetOrdinal("LastReminderDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastReminderDate")),
                EscalateToUserId = reader.IsDBNull(reader.GetOrdinal("EscalateToUserId")) ? null : reader.GetString(reader.GetOrdinal("EscalateToUserId")),
                EscalatedDate = reader.IsDBNull(reader.GetOrdinal("EscalatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EscalatedDate")),
                OriginalDueDate = reader.IsDBNull(reader.GetOrdinal("OriginalDueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OriginalDueDate")),
                ExtensionReason = reader.IsDBNull(reader.GetOrdinal("ExtensionReason")) ? null : reader.GetString(reader.GetOrdinal("ExtensionReason")),
                ExtendedBy = reader.IsDBNull(reader.GetOrdinal("ExtendedBy")) ? null : reader.GetString(reader.GetOrdinal("ExtendedBy")),
                ExtensionDate = reader.IsDBNull(reader.GetOrdinal("ExtensionDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExtensionDate")),
                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : reader.GetString(reader.GetOrdinal("ApprovedBy")),
                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ApprovedDate")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),

            };
        }

    }

    // ==================== INSTITUTION MAPPER ====================

    public static class InstitutionMapper
    {
        public static Institution MapToInstitution(SqlDataReader reader)
        {
            return new Institution
            {
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                ShortName = reader.IsDBNull(reader.GetOrdinal("ShortName")) ? null : reader.GetString(reader.GetOrdinal("ShortName")),
                Type = (InstitutionType)reader.GetInt32(reader.GetOrdinal("Type")),
                Adress = reader.IsDBNull(reader.GetOrdinal("Adress")) ? null : reader.GetString(reader.GetOrdinal("Adress")),
                City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
                PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader.GetString(reader.GetOrdinal("PostalCode")),
                Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                Fax = reader.IsDBNull(reader.GetOrdinal("Fax")) ? null : reader.GetString(reader.GetOrdinal("Fax")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                Website = reader.IsDBNull(reader.GetOrdinal("Website")) ? null : reader.GetString(reader.GetOrdinal("Website")),
                ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ? null : reader.GetString(reader.GetOrdinal("ContactPerson")),
                ContactPosition = reader.IsDBNull(reader.GetOrdinal("ContactPosition")) ? null : reader.GetString(reader.GetOrdinal("ContactPosition")),
                ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone")),
                ContactEmail = reader.IsDBNull(reader.GetOrdinal("ContactEmail")) ? null : reader.GetString(reader.GetOrdinal("ContactEmail")),
                TaxCode = reader.IsDBNull(reader.GetOrdinal("TaxCode")) ? null : reader.GetString(reader.GetOrdinal("TaxCode")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }

    // ==================== CLASSIFICATION MAPPER ====================

    public static class ClassificationMapper
    {
        public static Classification MapToClassification(SqlDataReader reader)
        {
            return new Classification
            {
                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Level = (AccessLevel)reader.GetInt32(reader.GetOrdinal("Level")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                RetentionYears = reader.GetInt32(reader.GetOrdinal("RetentionYears")),
                RequiresApproval = reader.GetBoolean(reader.GetOrdinal("RequiresApproval")),
                MinimumRoleRequired = reader.IsDBNull(reader.GetOrdinal("MinimumRoleRequired"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("MinimumRoleRequired")),
                AllowPrint = reader.GetBoolean(reader.GetOrdinal("AllowPrint")),
                AllowDownload = reader.GetBoolean(reader.GetOrdinal("AllowDownload")),
                AllowCopy = reader.GetBoolean(reader.GetOrdinal("AllowCopy")),
                EnableAuditLog = reader.GetBoolean(reader.GetOrdinal("EnableAuditLog")),
                ColorCode = reader.IsDBNull(reader.GetOrdinal("ColorCode"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ColorCode")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }

    // ==================== USER MAPPER ====================

    public static class UserMapper
    {
        public static ApplicationUser MapToApplicationUser(SqlDataReader reader)
        {
            return new ApplicationUser
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position")),
                Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedData = reader.IsDBNull(reader.GetOrdinal("ModifiedData")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedData"))
            };
        }
    }

    // ==================== PROTOCOL SETTINGS MAPPER ====================

    public static class ProtocolSettingsMapper
    {
        public static ProtocolSettings MapToProtocolSettings(SqlDataReader reader)
        {
            return new ProtocolSettings
            {
                ProtocolSettingsId = reader.GetInt32(reader.GetOrdinal("ProtocolSettingsId")),
                Year = reader.GetInt32(reader.GetOrdinal("Year")),
                IncomingStartNumber = reader.GetInt32(reader.GetOrdinal("IncomingStartNumber")),
                IncomingCurrentNumber = reader.GetInt32(reader.GetOrdinal("IncomingCurrentNumber")),
                IncomingEndNumber = reader.IsDBNull(reader.GetOrdinal("IncomingEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("IncomingEndNumber")),
                IncomingPrefix = reader.IsDBNull(reader.GetOrdinal("IncomingPrefix")) ? null : reader.GetString(reader.GetOrdinal("IncomingPrefix")),
                IncomingSuffix = reader.IsDBNull(reader.GetOrdinal("IncomingSuffix")) ? null : reader.GetString(reader.GetOrdinal("IncomingSuffix")),
                OutgoingStartNumber = reader.GetInt32(reader.GetOrdinal("OutgoingStartNumber")),
                OutgoingCurrentNumber = reader.GetInt32(reader.GetOrdinal("OutgoingCurrentNumber")),
                OutgoingEndNumber = reader.IsDBNull(reader.GetOrdinal("OutgoingEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("OutgoingEndNumber")),
                OutgoingPrefix = reader.IsDBNull(reader.GetOrdinal("OutgoingPrefix")) ? null : reader.GetString(reader.GetOrdinal("OutgoingPrefix")),
                OutgoingSuffix = reader.IsDBNull(reader.GetOrdinal("OutgoingSuffix")) ? null : reader.GetString(reader.GetOrdinal("OutgoingSuffix")),
                InternalStartNumber = reader.GetInt32(reader.GetOrdinal("InternalStartNumber")),
                InternalCurrentNumber = reader.GetInt32(reader.GetOrdinal("InternalCurrentNumber")),
                InternalEndNumber = reader.IsDBNull(reader.GetOrdinal("InternalEndNumber")) ? null : reader.GetInt32(reader.GetOrdinal("InternalEndNumber")),
                InternalPrefix = reader.IsDBNull(reader.GetOrdinal("InternalPrefix")) ? null : reader.GetString(reader.GetOrdinal("InternalPrefix")),
                InternalSuffix = reader.IsDBNull(reader.GetOrdinal("InternalSuffix")) ? null : reader.GetString(reader.GetOrdinal("InternalSuffix")),
                ProtocolNumberFormat = reader.GetString(reader.GetOrdinal("ProtocolNumberFormat")),
                NumberPadding = reader.GetInt32(reader.GetOrdinal("NumberPadding")),
                AutoResetYearly = reader.GetBoolean(reader.GetOrdinal("AutoResetYearly")),
                AllowManualEdit = reader.GetBoolean(reader.GetOrdinal("AllowManualEdit")),
                ShowYearInNumber = reader.GetBoolean(reader.GetOrdinal("ShowYearInNumber")),
                UseSeparatorSlash = reader.GetBoolean(reader.GetOrdinal("UseSeparatorSlash")),
                InstitutionName = reader.IsDBNull(reader.GetOrdinal("InstitutionName")) ? null : reader.GetString(reader.GetOrdinal("InstitutionName")),
                InstitutionCode = reader.IsDBNull(reader.GetOrdinal("InstitutionCode")) ? null : reader.GetString(reader.GetOrdinal("InstitutionCode")),
                InstitutionAddress = reader.IsDBNull(reader.GetOrdinal("InstitutionAddress")) ? null : reader.GetString(reader.GetOrdinal("InstitutionAddress")),
                InstitutionPhone = reader.IsDBNull(reader.GetOrdinal("InstitutionPhone")) ? null : reader.GetString(reader.GetOrdinal("InstitutionPhone")),
                InstitutionEmail = reader.IsDBNull(reader.GetOrdinal("InstitutionEmail")) ? null : reader.GetString(reader.GetOrdinal("InstitutionEmail")),
                InstitutionWebsite = reader.IsDBNull(reader.GetOrdinal("InstitutionWebsite")) ? null : reader.GetString(reader.GetOrdinal("InstitutionWebsite")),
                FiscalYearStart = reader.IsDBNull(reader.GetOrdinal("FiscalYearStart")) ? null : reader.GetDateTime(reader.GetOrdinal("FiscalYearStart")),
                FiscalYearEnd = reader.IsDBNull(reader.GetOrdinal("FiscalYearEnd")) ? null : reader.GetDateTime(reader.GetOrdinal("FiscalYearEnd")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsClosed = reader.GetBoolean(reader.GetOrdinal("IsClosed")),
                ClosedDate = reader.IsDBNull(reader.GetOrdinal("ClosedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ClosedDate")),
                ClosedBy = reader.IsDBNull(reader.GetOrdinal("ClosedBy")) ? null : reader.GetString(reader.GetOrdinal("ClosedBy")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
            };
        }
    }
}