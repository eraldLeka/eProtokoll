using eProtokoll.Controllers.Base;
using eProtokoll.Repositories.Document;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class OutgoingDocumentController : BaseOutgoingDocumentController
    {
        public OutgoingDocumentController(
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment,
            IProtocolNumberService protocolNumberService)
            : base(documentRepository, environment, protocolNumberService)
        {
        }
    }
}