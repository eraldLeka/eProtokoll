using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Services.Mappers
{
    // ================= DOCUMENT BASE =================

    public static class DocumentMapper
    {
        private static void MapBase(SqlDataReader reader, Document document)
        {
            document.DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId"));
            document.DocumentNumber = reader.GetInt32(reader.GetOrdinal("DocumentNumber"));
            document.Year = reader.GetInt32(reader.GetOrdinal("Year"));
            document.DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"));
            document.Subject = reader.GetString(reader.GetOrdinal("Subject"));

            int iContent = reader.GetOrdinal("Content");
            document.Content = reader.IsDBNull(iContent) ? null : reader.GetString(iContent);

            document.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));
            document.Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority"));

            int iHasAtt = reader.GetOrdinal("HasAttachments");
            document.HasAttachments = !reader.IsDBNull(iHasAtt) && reader.GetBoolean(iHasAtt);
            int iReqResp = reader.GetOrdinal("RequiresResponse");
            document.RequiresResponse = !reader.IsDBNull(iReqResp) && reader.GetBoolean(iReqResp);

            document.CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy"));
            document.CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"));
        }

        public static Document MapToDocument(SqlDataReader reader)
        {
            var document = new Document();
            MapBase(reader, document);
            return document;
        }

        public static IncomingDocument MapToIncomingDocument(SqlDataReader reader)
        {
            var document = new IncomingDocument();
            MapBase(reader, document);

            document.InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId"));
            document.SenderName = reader.GetString(reader.GetOrdinal("SenderName"));
            document.ReceivedDate = reader.GetDateTime(reader.GetOrdinal("ReceivedDate"));

            int iReceivedBy = reader.GetOrdinal("ReceivedBy");
            document.ReceivedBy = reader.IsDBNull(iReceivedBy) ? null : reader.GetInt32(iReceivedBy);

            int iOrigNum = reader.GetOrdinal("OriginalDocumentNumber");
            document.OriginalDocumentNumber = reader.IsDBNull(iOrigNum) ? null : reader.GetString(iOrigNum);

            int iOrigDate = reader.GetOrdinal("OriginalDocumentDate");
            document.OriginalDocumentDate = reader.IsDBNull(iOrigDate) ? null : reader.GetDateTime(iOrigDate);

            int iDeadline = reader.GetOrdinal("ResponseDeadline");
            document.ResponseDeadline = reader.IsDBNull(iDeadline) ? null : reader.GetDateTime(iDeadline);

            int iRespDate = reader.GetOrdinal("ResponseDate");
            document.ResponseDate = reader.IsDBNull(iRespDate) ? null : reader.GetDateTime(iRespDate);

            int iRespDocId = reader.GetOrdinal("ResponseDocumentId");
            document.ResponseDocumentId = reader.IsDBNull(iRespDocId) ? null : reader.GetInt32(iRespDocId);

            return document;
        }

        public static OutgoingDocument MapToOutgoingDocument(SqlDataReader reader)
        {
            var document = new OutgoingDocument();
            MapBase(reader, document);

            document.InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId"));
            document.RecipientName = reader.GetString(reader.GetOrdinal("RecipientName"));

            int iIsResp = reader.GetOrdinal("IsResponse");
            document.IsResponse = !reader.IsDBNull(iIsResp) && reader.GetBoolean(iIsResp);

            int iOrigInc = reader.GetOrdinal("OriginalIncomingDocumentId");
            document.OriginalIncomingDocumentId = reader.IsDBNull(iOrigInc) ? null : reader.GetInt32(iOrigInc);

            int iArchive = reader.GetOrdinal("ArchiveLocation");
            document.ArchiveLocation = reader.IsDBNull(iArchive) ? null : reader.GetString(iArchive);

            return document;
        }

        public static InternalDocument MapToInternalDocument(SqlDataReader reader)
        {
            var document = new InternalDocument();
            MapBase(reader, document);

            int iFrom = reader.GetOrdinal("FromDepartment");
            document.FromDepartment = reader.IsDBNull(iFrom) ? null : reader.GetString(iFrom);

            int iTo = reader.GetOrdinal("ToDepartment");
            document.ToDepartment = reader.IsDBNull(iTo) ? null : reader.GetString(iTo);

            return document;
        }
    }

    // ================= ATTACHMENT =================

    public static class AttachmentMapper
    {
        public static DocumentAttachment Map(SqlDataReader reader)
        {
            int iExt = reader.GetOrdinal("FileExtension");
            int iCt = reader.GetOrdinal("ContentType");
            int iDesc = reader.GetOrdinal("Description");

            return new DocumentAttachment
            {
                AttachmentId = reader.GetInt32(reader.GetOrdinal("AttachmentId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                FileExtension = reader.IsDBNull(iExt) ? null : reader.GetString(iExt),
                ContentType = reader.IsDBNull(iCt) ? null : reader.GetString(iCt),
                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                UploadedBy = reader.GetInt32(reader.GetOrdinal("UploadedBy")),
                Category = (FileCategory)reader.GetInt32(reader.GetOrdinal("Category")),
                Description = reader.IsDBNull(iDesc) ? null : reader.GetString(iDesc),
            };
        }
    }

    // ================= TRACKING =================

    public static class TrackingMapper
    {
        public static DocumentTracking Map(SqlDataReader reader)
        {
            int iDueDate = reader.GetOrdinal("DueDate");
            int iNotes = reader.GetOrdinal("Notes");
            int iCompDate = reader.GetOrdinal("CompletedDate");

            return new DocumentTracking
            {
                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                AssignedToUserId = reader.GetInt32(reader.GetOrdinal("AssignedToUserId")),
                AssignedByUserId = reader.GetInt32(reader.GetOrdinal("AssignedByUserId")),
                AssignedDate = reader.GetDateTime(reader.GetOrdinal("AssignedDate")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                DueDate = reader.IsDBNull(iDueDate) ? null : reader.GetDateTime(iDueDate),
                Notes = reader.IsDBNull(iNotes) ? null : reader.GetString(iNotes),
                CompletedDate = reader.IsDBNull(iCompDate) ? null : reader.GetDateTime(iCompDate),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            };
        }
    }

    // ================= USER =================

    public static class UserMapper
    {
        public static Users Map(SqlDataReader reader)
        {
            int iEmail = reader.GetOrdinal("Email");
            int iPos = reader.GetOrdinal("Position");
            int iDept = reader.GetOrdinal("Department");
            int iPhone = reader.GetOrdinal("PhoneNumber");
            int iActive = reader.GetOrdinal("IsActive");
            int iCreated = reader.GetOrdinal("CreatedDate");
            int iModified = reader.GetOrdinal("ModifiedDate");

            return new Users
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = reader.IsDBNull(iEmail) ? string.Empty : reader.GetString(iEmail),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Position = reader.IsDBNull(iPos) ? null : reader.GetString(iPos),
                Department = reader.IsDBNull(iDept) ? null : reader.GetString(iDept),
                PhoneNumber = reader.IsDBNull(iPhone) ? null : reader.GetString(iPhone),
                IsActive = !reader.IsDBNull(iActive) && reader.GetBoolean(iActive),
                CreatedDate = reader.IsDBNull(iCreated) ? DateTime.Now : reader.GetDateTime(iCreated),
                ModifiedDate = reader.IsDBNull(iModified) ? null : reader.GetDateTime(iModified)
            };
        }
    }

    // ================= INSTITUTION =================

    public static class InstitutionMapper
    {
        public static Institution Map(SqlDataReader reader)
        {
            int iShort = reader.GetOrdinal("ShortName");
            int iAddr = reader.GetOrdinal("Adress");
            int iPost = reader.GetOrdinal("PostalCode");
            int iCountry = reader.GetOrdinal("Country");
            int iPhone = reader.GetOrdinal("Phone");
            int iEmail = reader.GetOrdinal("Email");
            int iWeb = reader.GetOrdinal("Website");
            int iContact = reader.GetOrdinal("ContactPerson");
            int iContPos = reader.GetOrdinal("ContactPosition");
            int iContEmail = reader.GetOrdinal("ContactEmail");
            int iModDate = reader.GetOrdinal("ModifiedDate");
            int iCreatedBy = reader.GetOrdinal("CreatedBy");
            int iModBy = reader.GetOrdinal("ModifiedBy");

            return new Institution
            {
                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                ShortName = reader.IsDBNull(iShort) ? null : reader.GetString(iShort),
                Type = (InstitutionType)reader.GetInt32(reader.GetOrdinal("Type")),
                Adress = reader.IsDBNull(iAddr) ? null : reader.GetString(iAddr),
                PostalCode = reader.IsDBNull(iPost) ? null : reader.GetString(iPost),
                Country = reader.IsDBNull(iCountry) ? null : reader.GetString(iCountry),
                Phone = reader.IsDBNull(iPhone) ? null : reader.GetString(iPhone),
                Email = reader.IsDBNull(iEmail) ? null : reader.GetString(iEmail),
                Website = reader.IsDBNull(iWeb) ? null : reader.GetString(iWeb),
                ContactPerson = reader.IsDBNull(iContact) ? null : reader.GetString(iContact),
                ContactPosition = reader.IsDBNull(iContPos) ? null : reader.GetString(iContPos),
                ContactEmail = reader.IsDBNull(iContEmail) ? null : reader.GetString(iContEmail),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(iModDate) ? null : reader.GetDateTime(iModDate),
                CreatedBy = reader.IsDBNull(iCreatedBy) ? null : reader.GetString(iCreatedBy),
                ModifiedBy = reader.IsDBNull(iModBy) ? null : reader.GetString(iModBy)
            };
        }
    }
}