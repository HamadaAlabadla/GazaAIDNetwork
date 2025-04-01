using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.CycleAidService
{
    public interface ICycleAidService
    {
        Task<ResultResponse> CreateCycleAidAsync(CycleAid cycleAid, HttpContext httpContext);
        Task<List<CycleAidViewModel>> GetAllCycleAidsAsync(HttpContext httpContext);
        Task<CycleAid?> GetLastCycleAidActiveAsync(HttpContext httpContext);
        Task<ResultResponse> DeleteCycleAidAsync(string cycleAidId, HttpContext httpContext);
        Task<ResultResponse> UpdateStatusCycleAidAsync(string cycleAidId, CycleAidStatus cycleAidStatus, HttpContext httpContext);
    }
    public class CycleAidService : ICycleAidService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryAudit _repositoryAudit;
        public CycleAidService(ApplicationDbContext context,
            UserManager<User> userManager,
            IRepositoryAudit repositoryAudit)
        {
            _context = context;
            _userManager = userManager;
            _repositoryAudit = repositoryAudit;
        }
        public async Task<ResultResponse> CreateCycleAidAsync(CycleAid cycleAid, HttpContext httpContext)
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            //var LastCycleAid = _context.CycleAids;
            //if (LastCycleAid != null && LastCycleAid.Count() >0 && LastCycleAid.OrderByDescending(x => x.StartDate).Last().CycleAidStatus != CycleAidStatus.Done)
            //    return new ResultResponse()
            //    {
            //        Success = false,
            //        Message = "لا يمكن بدء دورة جديدة ,  أكمل الدورة الحالية للبدء بدورة جديدة",  
            //        Errors = new List<string>() { "لا يمكن بدء دورة جديدة ,  أكمل الدورة الحالية للبدء بدورة جديدة" }
            //    };
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            var existCycleAid = _context.CycleAids.FirstOrDefault(x => x.Id.ToString().Equals(cycleAid.Id.ToString())
                                || x.Name.Equals(cycleAid.Name));

            if (existCycleAid != null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "هذه الدورة مضافة مسبقا"
                };
            TimeZoneInfo palestineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Gaza"); // أو "Asia/Hebron"
            cycleAid.StartDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, palestineTimeZone);
            cycleAid.EndDate = null;
            cycleAid.DivisionId = (Guid)currentUser.DivisionId;
            cycleAid.CycleAidStatus = CycleAidStatus.Pending;

            _context.CycleAids.Add(cycleAid);
            var result = await _context.SaveChangesAsync();
            if (result < 0)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "حدث خطأ أثناء إضافة دورة التوزيع"
                };


            return new ResultResponse()
            {
                Success = true,
                Message = "تم إضافة الدورة بنجاح",
                result = cycleAid
            };

        }


        public async Task<ResultResponse> DeleteCycleAidAsync(string cycleAidId, HttpContext httpContext)
        {
            var cycleAid = await _context.CycleAids.Include(x => x.ProjectAids).FirstOrDefaultAsync(x => x.Id.ToString().Equals(cycleAidId));

            if (cycleAid == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "الدورة غير موجودة",
                    Errors = new List<string> { "ID الدورة غير صحيح" }
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
            if (!currentUser.DivisionId.Equals(cycleAid.DivisionId))
                return new ResultResponse()
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                };
            if (cycleAid.ProjectAids.Any())
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "لا يمكن حذف الدورة  , تحتوي بيانات لا يمكن حذفها"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.CycleAids.Remove(cycleAid);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.CycleAid,
                    RepoId = cycleAid.Id.ToString(),
                    Name = AuditName.Delete,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id,
                    Description = "تم حذف الدورة بنجاح"
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
                    Message = "تم حذف الدورة بنجاح"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل حذف الدورة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }



        public async Task<List<CycleAidViewModel>> GetAllCycleAidsAsync(HttpContext httpContext)
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new List<CycleAidViewModel>() { };
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));

            var cycleAids = _context.CycleAids
                .Include(x => x.Division)
                .Include(x => x.ProjectAids)
                .Where(x => x.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()))
                .ToList();

            var families = _context.Families.Include(x => x.Husband)
                .Where(x => x.Husband.DivisionId.ToString().Equals(currentUser.DivisionId.ToString())
                && !x.IsDeleted
                && x.MaritalStatus != MaritalStatus.traveler
                && x.StatusFamily == StatusFamily.accepted).ToList();
            var cycleAidViewModels = cycleAids.Select(x => new CycleAidViewModel()
            {
                CycleAidStatus = EnumHelper.GetDisplayName(x.CycleAidStatus),
                DivisionName = x.Division.Name,
                Name = x.Name,
                EndDate = x.EndDate != null ? ((DateTime)x.EndDate).ToString("d-M-yyyy HH:mm:ss") : "قيد التنفيذ",
                StartDate = x.StartDate.ToString("d-M-yyyy HH:mm:ss"),
                NumberOfBeneficiaries = _context.ProjectAids
                                            .Where(p => p.CycleAidId == x.Id) // ✅ Direct comparison, no ToString()
                                            .SelectMany(p => p.OrderAids) // ✅ Flatten OrderAids
                                            .Count(o => o.OrderAidStatus == OrderAidStatus.Delivered), // ✅ Count directly in databas
                Id = x.Id.ToString(),
                NumberOfFamilies = families.Count(),
            })
            .ToList();

            return cycleAidViewModels;
        }

        public async Task<CycleAid?> GetLastCycleAidActiveAsync(HttpContext httpContext)
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return null;
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            return _context.CycleAids.Where(x => x.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()))
                .OrderByDescending(x => x.StartDate)
                .Last(x => x.CycleAidStatus == CycleAidStatus.Start);
        }

        public async Task<ResultResponse> UpdateStatusCycleAidAsync(string cycleAidId, CycleAidStatus cycleAidStatus, HttpContext httpContext)
        {
            var cycleAid = _context.CycleAids.FirstOrDefault(x => x.Id.ToString().Equals(cycleAidId));
            if (cycleAid == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "هذا العنصر غير موجود"
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
            if (!currentUser.DivisionId.ToString().Equals(cycleAid.DivisionId.ToString()))
                return new ResultResponse()
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                };
            TimeZoneInfo palestineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Gaza"); // أو "Asia/Hebron"
            // التحقق من صلاحية تغيير الحالة
            switch (cycleAid.CycleAidStatus)
            {
                case CycleAidStatus.Start:
                    if (cycleAidStatus != CycleAidStatus.Pending)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Pending" };
                    cycleAid.StartDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, palestineTimeZone);
                    break;

                case CycleAidStatus.Done:
                    if (cycleAidStatus != CycleAidStatus.Start)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Accepted" };
                    cycleAid.EndDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, palestineTimeZone);
                    break;
                default:
                    return new ResultResponse { Success = false, Message = "حالة غير صالحة" };
            }

            // تحديث الحالة
            cycleAid.CycleAidStatus = cycleAidStatus;
            _context.CycleAids.Update(cycleAid);
            await _context.SaveChangesAsync();
            // Add audit log
            var auditLog = new AuditLog
            {
                EntityType = EntityType.CycleAid,
                RepoId = cycleAid.Id.ToString(),
                Name = AuditName.Update,
                CreatedDate = DateTime.UtcNow,
                AdminId = currentUser.Id,
                Description = "تم بدء التنفيذ لهذه الدورة"
            };
            var result = await _repositoryAudit.CreateAudit(auditLog);
            if (!result.Success)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = result.Message,
                };
            }
            return new ResultResponse
            {
                Success = true,
                Message = "تم تحديث الحالة بنجاح"
            };
        }

    }
}
