using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.EntityFrameworkCore;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services
{
    public interface IRepositoryAudit
    {
        Task<ResultResponse> CreateAudit(AuditLog Audit);
        Task<List<AuditLogViewModel>> GetAllAudit(string repoId, EntityType entityType);
    }

    public class RepositoryAudit : IRepositoryAudit
    {
        private readonly ApplicationDbContext _context;
        public RepositoryAudit(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultResponse> CreateAudit(AuditLog Audit)
        {
            if (Audit == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "Audit log cannot be null.",
                    Errors = new List<string> { "Invalid audit log data." }
                };
            }

            try
            {
                TimeZoneInfo palestineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Gaza"); // أو "Asia/Hebron"
                Audit.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, palestineTimeZone);
                await _context.AuditLogs.AddAsync(Audit);
                await _context.SaveChangesAsync();

                return new ResultResponse
                {
                    Success = true,
                    Message = "Audit log created successfully.",
                    result = Audit
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "Failed to create audit log.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<AuditLogViewModel>> GetAllAudit(string repoId, EntityType entityType)
        {
            try
            {
                var audits = await _context.AuditLogs.Include(x => x.Admin).Where(x => (x.RepoId.Equals(repoId) && x.EntityType == entityType))
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();
                var auditsViewModel = audits.Select(x => new AuditLogViewModel()
                {
                    Name = EnumHelper.GetDisplayName(x.Name),
                    AdminName = x.Admin.FullName,
                    DateCreate = x.CreatedDate.ToString("d-M-yyyy HH:mm:ss"),
                    Description = x.Description,
                }).ToList();
                return auditsViewModel;
            }
            catch (Exception ex)
            {
                return new List<AuditLogViewModel>();
            }
        }
    }
}
