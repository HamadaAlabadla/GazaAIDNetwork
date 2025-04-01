using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.CycleAidService
{
    public interface IInfoRepresentativeService
    {
        Task<ResultResponse> CreateInfoRepresentative(InfoRepresentative infoRepresentative, HttpContext httpContext);
    }
    public class InfoRepresentativeService : IInfoRepresentativeService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryAudit _repositoryAudit;
        public InfoRepresentativeService(ApplicationDbContext context,
            UserManager<User> userManager,
            IRepositoryAudit repositoryAudit)
        {
            _context = context;
            _userManager = userManager;
            _repositoryAudit = repositoryAudit;
        }

        public async Task<ResultResponse> CreateInfoRepresentative(InfoRepresentative infoRepresentative, HttpContext httpContext)
        {
            if (infoRepresentative == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب إدخال البيانات بشكل صحيح",
                    Errors = new List<string> { "بيانات المندوب لهذا المشروع غير صالحة" }
                };
            }
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.InfoRepresentatives.AddAsync(infoRepresentative);
                await _context.SaveChangesAsync();
                // Create an audit log entry
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.InfoRepresentative,
                    RepoId = infoRepresentative.Id.ToString(),
                    Name = AuditName.Create,
                    CreatedDate = DateTime.UtcNow,
                    Description = "تم إضافة معلومات المندوب بنجاح",
                    AdminId = currentUser.Id,

                };

                var result = await _repositoryAudit.CreateAudit(auditLog);
                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = result.Message,
                    };
                }
                await transaction.CommitAsync();

                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة معلومات المندوب بنجاح",
                    result = infoRepresentative
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل إضافة معلومات المندوب ",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
