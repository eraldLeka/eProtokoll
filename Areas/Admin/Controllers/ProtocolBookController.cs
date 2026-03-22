using eProtokoll.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class ProtocolBookController : BaseProtocolBookController
    {
        protected override string AreaName => "Admin";

        public ProtocolBookController(IConfiguration configuration) : base(configuration) { }
    }
}