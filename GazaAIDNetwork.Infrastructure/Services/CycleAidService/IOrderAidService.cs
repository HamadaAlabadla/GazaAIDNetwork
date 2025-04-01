using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.CycleAidService
{
    public interface IOrderAidService
    {
        Task<ResultResponse> CreateOrderAidAsync(OrderAid orderAid, HttpContext httpContext);
        Task<ResultResponse> GetAllOrderAidsByProjectAidIdAsync(string projectAidId,HttpRequest httpRequest, HttpContext httpContext);
        Task<ResultResponse> GetAllOrderAidsByFamilyIdAsync(string familyId,HttpRequest httpRequest, HttpContext httpContext);
        Task<ResultResponse> UpdateStatusOrderAidAsync(string orderAidId,OrderAidStatus orderAidStatus, HttpContext httpContext);
    }
    public class OrderAidService : IOrderAidService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryAudit _repositoryAudit;
        public OrderAidService(ApplicationDbContext context,
            UserManager<User> userManager,
            IRepositoryAudit repositoryAudit)
        {
            _context = context;
            _userManager = userManager;
            _repositoryAudit = repositoryAudit;
        }

        public async Task<ResultResponse> CreateOrderAidAsync(OrderAid orderAid, HttpContext httpContext)
        {
            if (orderAid == null || orderAid.ProjectAidId == Guid.Empty || orderAid.FamilyId == Guid.Empty)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "البيانات غير مكتملة"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Fetch ProjectAid to check CycleAidId
                var projectAid = await _context.ProjectAids.FindAsync(orderAid.ProjectAidId);
                if (projectAid == null)
                {
                    return new ResultResponse { Success = false, Message = "المشروع غير موجود" };
                }
                var currentUser = await _userManager.GetUserAsync(httpContext.User);
                if (currentUser == null)
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "يجب تسجيل الدخول للإستمرار"
                    };
                currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
                if (!currentUser.DivisionId.Equals(projectAid.DivisionId))
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                    };
                // Check if the family already has an order in this project
                var existingOrder = await _context.OrderAids
                    .AnyAsync(o => o.FamilyId == orderAid.FamilyId && o.ProjectAidId.ToString().Equals(orderAid.ProjectAidId.ToString()));

                if (existingOrder)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "هذه العائلة حصلت بالفعل على مساعدة في هذا المشروع"
                    };
                }

                // If project is part of a CycleAid, check if the family already has an order in any project in that CycleAid
                if (projectAid.CycleAidId.HasValue)
                {
                    var cycleOrderExists = await _context.OrderAids
                        .AnyAsync(o => o.FamilyId == orderAid.FamilyId &&
                                       _context.ProjectAids.Any(p => p.Id.ToString().Equals(o.ProjectAidId.ToString())
                                       && p.CycleAidId.ToString().Equals(projectAid.CycleAidId.ToString())));

                    if (cycleOrderExists)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "هذه العائلة حصلت بالفعل على مساعدة في دورة المساعدات الحالية"
                        };
                    }
                }
                var ordersAid = _context.OrderAids.Where(x => x.ProjectAidId.ToString().Equals(orderAid.ProjectAidId.ToString())).ToList();
                var quantity = ordersAid.Select(x => x.Quantity).Sum();
                if((quantity+orderAid.Quantity) > projectAid.Quantity)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكنك الإضافة, لقد تجاوزت الكمية المطلوية"
                    };
                }
                // Add order if no duplicate found
                await _context.OrderAids.AddAsync(orderAid);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.OrderAid,
                    RepoId = orderAid.Id.ToString(),
                    Name = AuditName.Create,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id,
                    Description = "تم ترشيح هذه العائلة للإستفادة من هذا المشروع"
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

                return new ResultResponse { Success = true, Message = "تم إضافة المساعدة بنجاح", result = orderAid };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse { Success = false, Message = "فشل الإضافة", Errors = new List<string> { ex.Message } };
            }
        }

        public async Task<ResultResponse> GetAllOrderAidsByFamilyIdAsync(string familyId, HttpRequest httpRequest, HttpContext httpContext)
        {

            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var rolesForCurrentUser = (await _userManager.GetRolesAsync(currentUser)).ToList();
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            }

            var ordersAid = _context.OrderAids
                .Include(x => x.Family)
                .ThenInclude(c => c.Husband)
                .Include(x => x.ProjectAid)
                .ThenInclude(p => p.CycleAid)
                .Where(x => x.FamilyId.ToString().Equals(familyId) 
                && (x.OrderAidStatus == Enums.OrderAidStatus.GoToPickUp 
                    || x.OrderAidStatus == Enums.OrderAidStatus.Delivered
                    )
                );

            try
            {
                var draw = httpRequest.Form["draw"].FirstOrDefault();
                var excel = httpRequest.Form["excel"].FirstOrDefault();
                var start = httpRequest.Form["start"].FirstOrDefault();
                var length = httpRequest.Form["length"].FirstOrDefault();
                var sortColumn = httpRequest.Form["columns[" + httpRequest.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = httpRequest.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = httpRequest.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    ordersAid = sortColumn switch
                    {
                        "FamilyName" => sortColumnDirection.ToLower() == "desc"
                            ? ordersAid.OrderByDescending(f => f.Family.Husband.FullName)
                            : ordersAid.OrderBy(f => f.Family.Husband.FullName),
                        _ => ordersAid.OrderBy(sortColumn + " " + sortColumnDirection), // Default, no sorting
                    };

                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    ordersAid = ordersAid.Where(m => m.Family.Husband.FullName.Contains(searchValue)
                                                || m.Family.Husband.IdNumber.Contains(searchValue)
                                                || m.Family.WifeName.Contains(searchValue)
                                                || m.Family.WifeName.Contains(searchValue));
                }

                recordsTotal = ordersAid.Count();

                var data = ordersAid.Skip(skip).Take(pageSize).ToList();

                var ordersAidViewModels = ordersAid.Select(x => new OrderAidViewModel()
                {
                    Id = x.Id.ToString(),
                    CycleAidName = x.ProjectAid.CycleAid.Name,
                    HusbandIdNumber = x.Family.Husband.IdNumber,
                    HusbandName = x.Family.Husband.FullName,
                    MemebersNumber = x.Family.NumberMembers,
                    OrderAidStatus = EnumHelper.GetDisplayName(x.OrderAidStatus),
                    ProjectAidName = x.ProjectAid.Name,
                    Quantity = x.Quantity,
                    WifeIdNumber = x.Family.WifeIdNumber,
                    WifeName = x.Family.WifeName,
                })
                .ToList();

                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = ordersAidViewModels };

                return new ResultResponse
                {
                    Success = true,
                    result = jsonData
                };
            }
            catch (Exception ex)
            {
                throw;

            }

        }

        public async Task<ResultResponse> GetAllOrderAidsByProjectAidIdAsync(string projectAidId,HttpRequest httpRequest, HttpContext httpContext)
        {
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var rolesForCurrentUser = (await _userManager.GetRolesAsync(currentUser)).ToList();
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            }

            var ordersAid = _context.OrderAids
                .Include(x => x.Family)
                .ThenInclude(c => c.Husband)
                .Include(x => x.ProjectAid)
                .ThenInclude(p => p.CycleAid)
                .Where(x =>  x.ProjectAidId.ToString().Equals(projectAidId));

            try
            {
                var draw = httpRequest.Form["draw"].FirstOrDefault();
                var excel = httpRequest.Form["excel"].FirstOrDefault();
                var start = httpRequest.Form["start"].FirstOrDefault();
                var length = httpRequest.Form["length"].FirstOrDefault();
                var sortColumn = httpRequest.Form["columns[" + httpRequest.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = httpRequest.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = httpRequest.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    ordersAid = sortColumn switch
                    {
                        "FamilyName" => sortColumnDirection.ToLower() == "desc"
                            ? ordersAid.OrderByDescending(f => f.Family.Husband.FullName)
                            : ordersAid.OrderBy(f => f.Family.Husband.FullName),
                        _ => ordersAid.OrderBy(sortColumn + " " + sortColumnDirection), // Default, no sorting
                    };

                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    ordersAid = ordersAid.Where(m => m.Family.Husband.FullName.Contains(searchValue)
                                                || m.Family.Husband.IdNumber.Contains(searchValue)
                                                || m.Family.WifeName.Contains(searchValue)
                                                || m.Family.WifeName.Contains(searchValue));
                }

                recordsTotal = ordersAid.Count();

                var data = ordersAid.Skip(skip).Take(pageSize).ToList();

                var ordersAidViewModels = ordersAid.Select(x => new OrderAidViewModel()
                {
                    Id = x.Id.ToString(),
                    CycleAidName = x.ProjectAid.CycleAid.Name,
                    HusbandIdNumber = x.Family.Husband.IdNumber,
                    HusbandName = x.Family.Husband.FullName,
                    MemebersNumber = x.Family.NumberMembers,
                    OrderAidStatus = EnumHelper.GetDisplayName(x.OrderAidStatus),
                    ProjectAidName = x.ProjectAid.Name,
                    Quantity = x.Quantity,
                    WifeIdNumber = x.Family.WifeIdNumber,
                    WifeName = x.Family.WifeName,
                })
                .ToList();

                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = ordersAidViewModels };

                return new ResultResponse
                {
                    Success = true,
                    result = jsonData
                };
            }
            catch (Exception ex)
            {
                throw;

            }

        }

        public async Task<ResultResponse> UpdateStatusOrderAidAsync(string orderAidId, OrderAidStatus orderAidStatus, HttpContext httpContext)
        {
            var orderAid = _context.OrderAids.Include(x => x.ProjectAid).FirstOrDefault(x => x.Id.ToString().Equals(orderAidId));
            if (orderAid == null)
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
            if (!currentUser.DivisionId.ToString().Equals(orderAid.ProjectAid.DivisionId.ToString()))
                return new ResultResponse()
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                };
            var description = "";
            // التحقق من صلاحية تغيير الحالة
            switch (orderAid.OrderAidStatus)
            {
                case OrderAidStatus.Pending:
                    if (orderAidStatus != OrderAidStatus.Rejected && orderAidStatus != OrderAidStatus.Accepted)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Pending" };
                    break;

                case OrderAidStatus.Accepted:
                    if (orderAidStatus != OrderAidStatus.GoToPickUp)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Accepted" };
                    description = "تم إعتماد هذه الأسرة لتلقي المساعدة , توجه للاستلام";
                    break;

                case OrderAidStatus.GoToPickUp:
                    if (orderAidStatus != OrderAidStatus.Delivered)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من GoToPickup" };
                    description = "تم إستلام هذه المساعدة";
                    break;

                case OrderAidStatus.Rejected:
                    if (orderAidStatus != OrderAidStatus.Pending)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Rejected" };
                    break;

                default:
                    return new ResultResponse { Success = false, Message = "حالة غير صالحة" };
            }

            // تحديث الحالة
            orderAid.OrderAidStatus = orderAidStatus;
            _context.OrderAids.Update(orderAid);
            await _context.SaveChangesAsync();
            // Add audit log
            var auditLog = new AuditLog
            {
                EntityType = EntityType.OrderAid,
                RepoId = orderAid.Id.ToString(),
                Name = AuditName.Update,
                CreatedDate = DateTime.UtcNow,
                AdminId = currentUser.Id,
                Description = description
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
