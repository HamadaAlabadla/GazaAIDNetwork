using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.Services.UserService;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.DivisionService
{
    public interface IDivisionService
    {
        Task<ResultResponse> CreateDivisionAsync(Division division, HttpContext httpContext);
        Task<ResultResponse> GetDivisionAsync(string divisionId);
        Task<ResultResponse> UpdateDivisionAsync(Division division, HttpContext httpContext);
        Task<ResultResponse> ActiveDivisionAsync(string divisionId, HttpContext httpContext);
        Task<ResultResponse> DeleteDivisionAsync(string divisionId, HttpContext httpContext);
        Task<List<DivisionViewModel>> GetAllDivisionsAsync();
    }

    public class DivisionService : IDivisionService
    {
        private readonly IRepositoryAudit _repositoryAudit;
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public DivisionService(IRepositoryAudit repositoryAudit,
            ApplicationDbContext context,
            UserManager<User> userManager,
            IUserService userService)
        {
            _repositoryAudit = repositoryAudit;
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        public async Task<ResultResponse> ActiveDivisionAsync(string divisionId, HttpContext httpContext)
        {
            var division = await _context.Divisions.FirstOrDefaultAsync(x => x.Id.ToString().Equals(divisionId));

            if (division == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "الشعبة غير موجودة",
                    Errors = new List<string> { "Id الخاص بالشعبة غير متاح" }
                };
            }

            // Check if division is already active (not deleted)
            if (!division.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "الشعبة فعالة مسبقا",
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reactivate division by setting IsDeleted to false
                division.IsDeleted = false;

                _context.Divisions.Update(division);
                await _context.SaveChangesAsync();
                var admin = await _userManager.GetUserAsync(httpContext.User);
                // Add audit log for activation
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Division,
                    RepoId = division.Id.ToString(),
                    Name = AuditName.ReActive,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Description = "تم إعادة تفعيل الشعبة بنجاح"
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
                    Message = "تم إعادة تفعيل الشعبة بنجاح",
                    result = division,

                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل إعادة تفعيل الشعبة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ResultResponse> CreateDivisionAsync(Division division, HttpContext httpContext)
        {
            if (division == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب إدخال البيانات بشكل صحيح",
                    Errors = new List<string> { "بيانات الشعبة غير صالحة" }
                };
            }

            if ((await _context.Divisions.FirstOrDefaultAsync(x => x.Name.Equals(division.Name))) != null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "اسم الشعبة موجود مسبقا",
                    Errors = new List<string> { "يجب عدم تكرار اسم الشعبة" }
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                division.IsDeleted = false;
                await _context.Divisions.AddAsync(division);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(httpContext.User);
                // Create an audit log entry
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Division,
                    RepoId = division.Id.ToString(),
                    Name = AuditName.Create,
                    CreatedDate = DateTime.UtcNow,
                    Description = "تم إضافة الشعبة بنجاح",
                    AdminId = user.Id,

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
                    Message = "تم إضافة الشعبة بنجاح",
                    result = division
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل إضافة الشعبة ",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ResultResponse> DeleteDivisionAsync(string divisionId, HttpContext httpContext)
        {
            var division = await _context.Divisions.FirstOrDefaultAsync(x => x.Id.ToString().Equals(divisionId));

            if (division == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "الشعبة غير موجودة",
                    Errors = new List<string> { "ID الشعبة غير صحيح" }
                };
            }

            if (division.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "تم حذف الشعبة مسبقا"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mark as deleted instead of removing
                division.IsDeleted = true;

                _context.Divisions.Update(division);
                await _context.SaveChangesAsync();
                var admin = await _userManager.GetUserAsync(httpContext.User);
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Division,
                    RepoId = division.Id.ToString(),
                    Name = AuditName.Delete,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Description = "تم حذف الشعبة بنجاح"
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
                    Message = "تم حذف الشعبة بنجاح"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل حذف الشعبة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<DivisionViewModel>> GetAllDivisionsAsync()
        {
            var divisions = await _context.Divisions
                   .ToListAsync();
            var divisionViewModels = new List<DivisionViewModel>();
            foreach (var division in divisions)
            {
                var divisionViewModel = new DivisionViewModel()
                {
                    Id = division.Id,
                    AuditLogs = await _repositoryAudit.GetAllAudit(division.Id.ToString(), EntityType.Division),
                    IsDeleted = division.IsDeleted,
                    Name = division.Name,
                    Users = await _userService.GetAllUsersByDivisionIdAsync(division.Id.ToString())
                };
                divisionViewModels.Add(divisionViewModel);

            }
            return divisionViewModels;
        }

        public async Task<ResultResponse> GetDivisionAsync(string divisionId)
        {
            var division = await _context.Divisions
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(divisionId));

            if (division == null)
                return new ResultResponse { Success = false, Message = "الشعبة غير موجودة" };

            return new ResultResponse
            {
                Success = true,
                Message = "تم جلب بيانات الشعبة بنجاح",
                result = division
            };
        }


        public async Task<ResultResponse> UpdateDivisionAsync(Division division, HttpContext httpContext)
        {
            var existingDivision = await _context.Divisions.FindAsync(division.Id);

            if (existingDivision == null || existingDivision.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "الشعبة غير موجودة أو تم حذفها",
                    Errors = new List<string> { "تأكد من صحة البيانات" }
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update fields
                existingDivision.Name = division.Name;

                _context.Divisions.Update(existingDivision);
                await _context.SaveChangesAsync();

                var admin = await _userManager.GetUserAsync(httpContext.User);
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Division,
                    RepoId = existingDivision.Id.ToString(),
                    Name = AuditName.Update,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Description = "Division updated successfully."
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
                    Message = "تم تحديث بيانات الشعبة بنجاح",
                    result = existingDivision
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل تحديث بيانات الشعبة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
