using GazaAIDNetwork.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Web.Controllers
{
    public class AuditController : Controller
    {
        private readonly IRepositoryAudit _repositoryAudit;
        public AuditController(IRepositoryAudit repositoryAudit)
        {
            _repositoryAudit = repositoryAudit;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAudits(string repoId, string type)
        {
            // Ensure Enum parsing works for "Family" or other types
            if (Enum.TryParse(type, out EntityType entityType))
            {
                // Get the audits from the repository
                var audits = await _repositoryAudit.GetAllAudit(repoId, entityType);
                return Ok(audits);
            }

            // Return a bad request if the type is not valid
            return BadRequest("Invalid type provided.");
        }


    }
}
