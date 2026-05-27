using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Services.Mappers
{
    // ================= DOCUMENT BASE =================

    public static class DocumentMapper
    {
        private static int? TryGetOrdinal(SqlDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private static void MapBase(SqlDataReader reader, Document document)
        {
            document.DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId"));
            document.DocumentNumber = reader.GetInt32(reader.GetOrdinal("DocumentNumber"));
            document.Year = reader.GetInt32(reader.GetOrdinal("Year"));
            document.DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"));
            document.Subject = reader.GetString(reader.GetOrdinal("Subject"));

            document.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));
            document.Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority"));

            int iHasAtt = reader.GetOrdinal("HasAttachments");
            document.HasAttachments = !reader.IsDBNull(iHasAtt) && reader.GetBoolean(iHasAtt);
            int iReqResp = reader.GetOrdinal("RequiresResponse");
            document.RequiresResponse = !reader.IsDBNull(iReqResp) && reader.GetBoolean(iReqResp);

            document.CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy"));
            document.CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"));

            var iCreatorFirstName = TryGetOrdinal(reader, "CreatorFirstName");
            var iCreatorLastName = TryGetOrdinal(reader, "CreatorLastName");
            var iCreatorUserName = TryGetOrdinal(reader, "CreatorUserName");

            if (iCreatorFirstName.HasValue || iCreatorLastName.HasValue || iCreatorUserName.HasValue)
            {
                document.Creator = new Users
                {
                    Id = document.CreatedBy,
                    FirstName = iCreatorFirstName.HasValue && !reader.IsDBNull(iCreatorFirstName.Value)
                        ? reader.GetString(iCreatorFirstName.Value)
                        : string.Empty,
                    LastName = iCreatorLastName.HasValue && !reader.IsDBNull(iCreatorLastName.Value)
                        ? reader.GetString(iCreatorLastName.Value)
                        : string.Empty,
                    UserName = iCreatorUserName.HasValue && !reader.IsDBNull(iCreatorUserName.Value)
                        ? reader.GetString(iCreatorUserName.Value)
                        : string.Empty,
                    PasswordHash = string.Empty,
                    Email = string.Empty
                };
            }

            var iLatestAttachmentPath = TryGetOrdinal(reader, "LatestAttachmentPath");
            if (iLatestAttachmentPath.HasValue && !reader.IsDBNull(iLatestAttachmentPath.Value))
            {
                var iLatestAttachmentId = TryGetOrdinal(reader, "LatestAttachmentId");
                var iLatestAttachmentName = TryGetOrdinal(reader, "LatestAttachmentName");
                var iLatestAttachmentExtension = TryGetOrdinal(reader, "LatestAttachmentExtension");
                var iLatestAttachmentSize = TryGetOrdinal(reader, "LatestAttachmentSize");
                var iLatestAttachmentUploadedDate = TryGetOrdinal(reader, "LatestAttachmentUploadedDate");
                var iLatestAttachmentUploadedBy = TryGetOrdinal(reader, "LatestAttachmentUploadedBy");
                var iLatestAttachmentCategory = TryGetOrdinal(reader, "LatestAttachmentCategory");

                document.Attachments = new List<DocumentAttachment>
                {
                    new DocumentAttachment
                    {
                        AttachmentId = iLatestAttachmentId.HasValue && !reader.IsDBNull(iLatestAttachmentId.Value)
                            ? reader.GetInt32(iLatestAttachmentId.Value)
                            : 0,
                        DocumentId = document.DocumentId,
                        OriginalFileName = iLatestAttachmentName.HasValue && !reader.IsDBNull(iLatestAttachmentName.Value)
                            ? reader.GetString(iLatestAttachmentName.Value)
                            : "Dokument",
                        FilePath = reader.GetString(iLatestAttachmentPath.Value),
                        FileExtension = iLatestAttachmentExtension.HasValue && !reader.IsDBNull(iLatestAttachmentExtension.Value)
                            ? reader.GetString(iLatestAttachmentExtension.Value)
                            : null,
                        FileSize = iLatestAttachmentSize.HasValue && !reader.IsDBNull(iLatestAttachmentSize.Value)
                            ? reader.GetInt64(iLatestAttachmentSize.Value)
                            : 0,
                        UploadedDate = iLatestAttachmentUploadedDate.HasValue && !reader.IsDBNull(iLatestAttachmentUploadedDate.Value)
                            ? reader.GetDateTime(iLatestAttachmentUploadedDate.Value)
                            : DateTime.UtcNow,
                        UploadedBy = iLatestAttachmentUploadedBy.HasValue && !reader.IsDBNull(iLatestAttachmentUploadedBy.Value)
                            ? reader.GetInt32(iLatestAttachmentUploadedBy.Value)
                            : 0,
                        Category = iLatestAttachmentCategory.HasValue && !reader.IsDBNull(iLatestAttachmentCategory.Value)
                            ? (FileCategory)reader.GetInt32(iLatestAttachmentCategory.Value)
                            : FileCategory.PDF,
                        FileHash = string.Empty
                    }
                };
                document.HasAttachments = true;
            }
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

            int iInstitutionId = reader.GetOrdinal("InstitutionId");
            document.InstitutionId = reader.IsDBNull(iInstitutionId) ? 0 : reader.GetInt32(iInstitutionId);
            int iSenderName = reader.GetOrdinal("SenderName");
            document.SenderName = reader.IsDBNull(iSenderName) ? string.Empty : reader.GetString(iSenderName);

            int iReceivedDate = reader.GetOrdinal("ReceivedDate");
            document.ReceivedDate = reader.IsDBNull(iReceivedDate)
                ? document.CreatedDate
                : reader.GetDateTime(iReceivedDate);

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

            var iInstitutionName = TryGetOrdinal(reader, "InstitutionName");
            if (iInstitutionName.HasValue && !reader.IsDBNull(iInstitutionName.Value))
            {
                document.Institution ??= new Institution();
                document.Institution.Name = reader.GetString(iInstitutionName.Value);
            }

            var iInstitutionAdress = TryGetOrdinal(reader, "InstitutionAdress");
            if (iInstitutionAdress.HasValue && !reader.IsDBNull(iInstitutionAdress.Value))
            {
                document.Institution ??= new Institution();
                document.Institution.Adress = reader.GetString(iInstitutionAdress.Value);
            }

            return document;
        }

        public static OutgoingDocument MapToOutgoingDocument(SqlDataReader reader)
        {
            var document = new OutgoingDocument();
            MapBase(reader, document);

            int iInstitutionId = reader.GetOrdinal("InstitutionId");
            document.InstitutionId = reader.IsDBNull(iInstitutionId) ? 0 : reader.GetInt32(iInstitutionId);
            int iRecipientName = reader.GetOrdinal("RecipientName");
            document.RecipientName = reader.IsDBNull(iRecipientName) ? string.Empty : reader.GetString(iRecipientName);

            int iIsResp = reader.GetOrdinal("IsResponse");
            document.IsResponse = !reader.IsDBNull(iIsResp) && reader.GetBoolean(iIsResp);

            int iOrigInc = reader.GetOrdinal("OriginalIncomingDocumentId");
            document.OriginalIncomingDocumentId = reader.IsDBNull(iOrigInc) ? null : reader.GetInt32(iOrigInc);

            int iArchive = reader.GetOrdinal("ArchiveLocation");
            document.ArchiveLocation = reader.IsDBNull(iArchive) ? null : reader.GetString(iArchive);

            var iInstitutionName = TryGetOrdinal(reader, "InstitutionName");
            if (iInstitutionName.HasValue && !reader.IsDBNull(iInstitutionName.Value))
            {
                document.Institution ??= new Institution();
                document.Institution.Name = reader.GetString(iInstitutionName.Value);
            }

            var iInstitutionAdress = TryGetOrdinal(reader, "InstitutionAdress");
            if (iInstitutionAdress.HasValue && !reader.IsDBNull(iInstitutionAdress.Value))
            {
                document.Institution ??= new Institution();
                document.Institution.Adress = reader.GetString(iInstitutionAdress.Value);
            }

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

            int iIsResp = reader.GetOrdinal("IsResponse");
            document.IsResponse = !reader.IsDBNull(iIsResp) && reader.GetBoolean(iIsResp);

            int iOrigInt = reader.GetOrdinal("OriginalInternalDocumentId");
            document.OriginalInternalDocumentId = reader.IsDBNull(iOrigInt) ? null : reader.GetInt32(iOrigInt);

            int iRespDate = reader.GetOrdinal("ResponseDate");
            document.ResponseDate = reader.IsDBNull(iRespDate) ? null : reader.GetDateTime(iRespDate);

            int iRespDocId = reader.GetOrdinal("ResponseDocumentId");
            document.ResponseDocumentId = reader.IsDBNull(iRespDocId) ? null : reader.GetInt32(iRespDocId);

            return document;
        }
    }

    // ================= ATTACHMENT =================

    public static class AttachmentMapper
    {
        public static DocumentAttachment Map(SqlDataReader reader)
        {
            int iExt = reader.GetOrdinal("FileExtension");
            int iDesc = reader.GetOrdinal("Description");

            return new DocumentAttachment
            {
                AttachmentId = reader.GetInt32(reader.GetOrdinal("AttachmentId")),
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                FileExtension = reader.IsDBNull(iExt) ? null : reader.GetString(iExt),
                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                UploadedBy = reader.GetInt32(reader.GetOrdinal("UploadedBy")),
                Category = (FileCategory)reader.GetInt32(reader.GetOrdinal("Category")),
                Description = reader.IsDBNull(iDesc) ? null : reader.GetString(iDesc),
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
            int iActive = reader.GetOrdinal("IsActive");

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
                IsActive = !reader.IsDBNull(iActive) && reader.GetBoolean(iActive),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(iModDate) ? null : reader.GetDateTime(iModDate),
                CreatedBy = reader.IsDBNull(iCreatedBy) ? null : reader.GetString(iCreatedBy),
                ModifiedBy = reader.IsDBNull(iModBy) ? null : reader.GetString(iModBy)
            };
        }
    }
}