using GazaAIDNetwork.Core.Dtos;using GazaAIDNetwork.Core.Enums;using GazaAIDNetwork.EF.Data;using GazaAIDNetwork.EF.Models;using GazaAIDNetwork.Infrastructure.Respons;using GazaAIDNetwork.Infrastructure.ViewModels;using Microsoft.AspNetCore.Http;using Microsoft.AspNetCore.Identity;using Microsoft.EntityFrameworkCore;using static GazaAIDNetwork.Core.Enums.Enums;namespace GazaAIDNetwork.Infrastructure.Services.UserService{    public interface IUserService    {        Task<ResultResponse> CreateUserAsync(UserDto userDto, HttpContext HttpContext);        Task<ResultResponse> UpdateUserAsync(UserDto userDto, HttpContext HttpContext);        Task<ResultResponse> DeleteUserAsync(string userId, HttpContext httpContext);        Task<ResultResponse> ResetPasswordUserAsync(string userId, HttpContext httpContext);        Task<ResultResponse> ResetPasswordUserByFamilyIdAsync(string familyId, HttpContext httpContext);        Task<ResultResponse> ReactivateUserAsync(string userId, HttpContext httpContext);        Task<ResultResponse> GetUserAsync(string userId);        Task<ResultResponse> GetUserByContextAsync(HttpContext httpContext);        Task<List<UserViewModel>> GetAllUsersAsync();        Task<List<UserViewModel>> GetAllUsersByDivisionIdAsync(string divisionId);        Task<List<UserViewModel>> GetAllRepresentativesAsync(string divisionId, HttpContext httpContext);    }    public class UserService : IUserService    {        private readonly ApplicationDbContext _context;        private readonly UserManager<User> _userManager;        private readonly RoleManager<IdentityRole> _roleManager;        private readonly IRepositoryAudit _repositoryAudit;        public UserService(ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,            UserManager<User> userManager,            IRepositoryAudit repositoryAudit)        {            _context = context;            _roleManager = roleManager;            _userManager = userManager;            _repositoryAudit = repositoryAudit;        }        public async Task<ResultResponse> GetUserAsync(string userId)
        {
            var user = await _context.Users.Include(x => x.Division)
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(userId));

            if (user == null)
                return new ResultResponse { Success = false, Message = "المستخدم غير موجودة" };
            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var userViewModel = new UserDto()
            {
                Id = userId,
                DivisionId = user.DivisionId.ToString(),
                FullName = user.FullName,
                IdNumber = user.IdNumber,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.Select(x => (int)Enum.Parse(typeof(Role), x.ToString())).ToArray()
            };
            return new ResultResponse
            {
                Success = true,
                Message = "تم جلب بيانات المستخدم بنجاح",
                result = userViewModel
            };
        }        public async Task<ResultResponse> CreateUserAsync(UserDto userDto, HttpContext httpContext)        {
            // Get the current user's roles
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "المستخدم غير موجود أو تم حذفه."
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Convert userDto role to Enum for easy comparison
            var invalidRoles = userDto.Roles.Where(role => !Enum.IsDefined(typeof(Role), role)).ToList();

            if (invalidRoles.Any())
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"Invalid roles: {string.Join(", ", invalidRoles)}."
                };
            }

            // Proceed with valid roles
            List<Role> validRoles = userDto.Roles.Select(role => (Role)role).ToList();


            Guid? divisionId = new Guid();

            // Check if the current user is allowed to create a new user with the requested role
            if (currentRoles.Contains(Role.superadmin.ToString()))            {
                // Superadmin can add any user with any role
                if (Guid.TryParse(userDto.DivisionId, out var parsedDivisionId))
                {
                    divisionId = parsedDivisionId;
                }
                else
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "خلل في تحديد الشعبة",
                    };
                }
            }            else if (currentRoles.Contains(Role.admin.ToString()))            {
                // Admin cannot add a superadmin
                if (validRoles.Contains(Role.superadmin))                {                    return new ResultResponse                    {                        Success = false,                        Message = "Admin cannot add a Superadmin user."                    };                }                if (!currentUser.Division.Id.Equals(userDto.DivisionId))
                {
                    return new ResultResponse                    {                        Success = false,                        Message = "لا يمكن الإضافة لشعبة أخرى"                    };
                }                divisionId = currentUser.DivisionId;            }            else if (currentRoles.Contains(Role.representative.ToString()))            {                if (!currentUser.Division.Id.ToString().Equals(userDto.DivisionId))
                {
                    return new ResultResponse                    {                        Success = false,                        Message = "لا يمكن الإضافة لشعبة أخرى"                    };
                }
                // Representative can only add a family role
                if (!validRoles.Contains(Role.family) && validRoles.Count() > 1)                {                    return new ResultResponse                    {                        Success = false,                        Message = "Representative can only add a Family user."                    };                }                divisionId = currentUser.DivisionId;            }            else if (currentRoles.Contains(Role.supervisor.ToString()) || currentRoles.Contains(Role.family.ToString()))            {
                // Supervisor and Family roles cannot add users
                return new ResultResponse                {                    Success = false,                    Message = "You are not authorized to add users."                };            }


            // Using transaction for user creation and audit log
            var existingTransaction = _context.Database.CurrentTransaction;
            using (var transaction = existingTransaction ?? await _context.Database.BeginTransactionAsync())
            {                try                {

                    // Create the user logic goes here
                    var newUser = new User                    {                        IdNumber = userDto.IdNumber,                        FullName = userDto.FullName,                        UserName = userDto.IdNumber,                        PhoneNumber = userDto.PhoneNumber,                        isDelete = false,                        DivisionId = divisionId,                    };                    var existUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName.Equals(newUser.UserName));                    if (existUser != null)
                    {
                        if (validRoles.Contains(Role.family) && validRoles.Count() == 1)
                        {
                            var resultRole = await EnsureRoleExistsAsync(validRoles.First());
                            if (!resultRole.Success)
                            {
                                await transaction.RollbackAsync();
                                return new ResultResponse
                                {
                                    Success = false,
                                    Message = resultRole.Message,
                                };
                            }
                            var resultUserRole = await _userManager.AddToRoleAsync(existUser, validRoles.First().ToString());

                            if (!resultUserRole.Succeeded)
                            {
                                await transaction.RollbackAsync();
                                return new ResultResponse
                                {
                                    Success = false,
                                    Message = " خلل في إضافة الصلاحية للمستخدم",
                                };
                            }
                            await transaction.CommitAsync();
                            return new ResultResponse
                            {
                                Success = true,
                                Message = "المستخدم موجود مسبقا , تم تحديث الصلاحية فقط",
                                result = existUser,
                            };
                        }
                        else if (validRoles.Any(x => x != Role.family))
                        {
                            foreach (Role role in validRoles)
                            {
                                var resultRole = await EnsureRoleExistsAsync(role);
                                if (!resultRole.Success)
                                {
                                    await transaction.RollbackAsync();
                                    return new ResultResponse
                                    {
                                        Success = false,
                                        Message = resultRole.Message,
                                    };
                                }
                                if (!await _userManager.IsInRoleAsync(existUser, role.ToString()))
                                {
                                    var resultUserNewRole = await _userManager.AddToRoleAsync(existUser, role.ToString());

                                    if (!resultUserNewRole.Succeeded)
                                    {
                                        return new ResultResponse
                                        {
                                            Success = false,
                                            Message = string.Join(", ", resultUserNewRole.Errors.Select(e => e.Description))
                                        };
                                    }
                                }
                                await transaction.CommitAsync();
                                return new ResultResponse
                                {
                                    Success = true,
                                    Message = "تم تحديث صلاحية المستخدم"
                                };
                            }
                        }
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "المستخدم موجود بالفعل",
                        };

                    }                    var result = await _userManager.CreateAsync(newUser, newUser.IdNumber);                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();                        return new ResultResponse                        {                            Success = false,                            Message = "خلل في إضافة المستخدم",                        };
                    }                    foreach (Role role in validRoles)
                    {
                        var resultRole = await EnsureRoleExistsAsync(role);
                        if (!resultRole.Success)
                        {
                            await transaction.RollbackAsync();
                            return new ResultResponse
                            {
                                Success = false,
                                Message = resultRole.Message,
                            };
                        }
                        var resultUserRole = await _userManager.AddToRoleAsync(newUser, role.ToString());

                        if (!resultUserRole.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            return new ResultResponse
                            {
                                Success = false,
                                Message = " خلل في إضافة الصلاحية للمستخدم",
                            };
                        }
                    }
                    // Audit log creation
                    var auditLog = new AuditLog                    {                        Id = Guid.NewGuid(),                        EntityType = EntityType.User,                        RepoId = newUser.Id.ToString(),                        Name = AuditName.Create,                        Description = "تم إضافة المستخدم بنجاح",                        CreatedDate = DateTime.UtcNow,                        AdminId = currentUserId                    };                    var resultAudit = await _repositoryAudit.CreateAudit(auditLog);                    if (!resultAudit.Success)
                    {
                        await transaction.RollbackAsync();                        return new ResultResponse                        {                            Success = false,                            Message = resultAudit.Message,                        };
                    }
                    // Commit the transaction if everything is successful
                    await transaction.CommitAsync();                    await _context.SaveChangesAsync();                    return new ResultResponse                    {                        Success = true,                        Message = "تمت إضافة المستخدم بنجاح",                        result = newUser                    };                }                catch (Exception ex)                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync();                    return new ResultResponse                    {                        Success = false,                        Message = $"{ex.Message} حدث خلل غير متوقع"                    };                }            }        }        public async Task<ResultResponse> EnsureRoleExistsAsync(Role role)
        {
            // Check if the role already exists in the system using RoleManager
            var roleExist = await _roleManager.RoleExistsAsync(role.ToString());

            if (roleExist)
            {
                // Role already exists
                return new ResultResponse
                {
                    Success = true,
                    Message = "Role already exists."
                };
            }

            try
            {
                // Role does not exist, create the role
                var newRole = new IdentityRole(role.ToString());

                var result = await _roleManager.CreateAsync(newRole);

                if (result.Succeeded)
                {
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "Role created successfully.",
                        result = newRole
                    };
                }
                else
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = $"Failed to create role: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"An error occurred while creating the role: {ex.Message}"
                };
            }
        }
        public async Task<ResultResponse> DeleteUserAsync(string userId, HttpContext httpContext)
        {


            var currentUserId = _userManager.GetUserId(httpContext.User);
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            // Check if the current user has sufficient roles to delete a user
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can delete any user
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin cannot delete superadmins
                var userToDelete = await _userManager.FindByIdAsync(userId);
                if (userToDelete != null && (await _userManager.GetRolesAsync(userToDelete)).Contains(Role.superadmin.ToString()))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن للمسؤول حذف مستخدم ذو صلاحية مشرف عام."
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))            {
                var userToDelete = await _userManager.FindByIdAsync(userId);

                // Representative can only add a family role
                if (userToDelete != null && !(await _userManager.GetRolesAsync(userToDelete)).Contains(Role.family.ToString()))                {                    return new ResultResponse                    {                        Success = false,                        Message = "يمكن للمندوب حذف العائلات فقط"                    };                }            }
            else
            {
                // Other roles are not authorized to delete users
                return new ResultResponse
                {
                    Success = false,
                    Message = "ليس لديك صلاحية لحذف المستخدمين."
                };
            }

            // Using transaction for soft delete and audit log
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the user to soft delete
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "المستخدم غير موجود."
                        };
                    }

                    // Perform soft delete by marking the user as deleted
                    user.isDelete = true;
                    var resultUpdate = await _userManager.UpdateAsync(user);
                    if (!resultUpdate.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "حدث خطأ أثناء حذف المستخدم."
                        };
                    }

                    // Create an audit log for the soft delete operation
                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = EntityType.User,
                        RepoId = user.Id.ToString(),
                        Name = AuditName.Delete,
                        Description = "تم حذف المستخدم (حذف منطقي).",
                        CreatedDate = DateTime.UtcNow,
                        AdminId = currentUserId
                    };
                    var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                    if (!resultAudit.Success)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = resultAudit.Message,
                        };
                    }

                    // Commit the transaction if everything is successful
                    await transaction.CommitAsync();
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تم حذف المستخدم بنجاح.",
                        result = user
                    };
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = $"حدث خطأ غير متوقع: {ex.Message}"
                    };
                }
            }
        }
        public async Task<ResultResponse> ReactivateUserAsync(string userId, HttpContext httpContext)
        {
            var currentUserId = _userManager.GetUserId(httpContext.User);
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            // Check if the current user has sufficient roles to reactivate a user
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can reactivate any user
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin cannot reactivate superadmins
                var userToReactivate = await _userManager.FindByIdAsync(userId);
                if (userToReactivate != null && (await _userManager.GetRolesAsync(userToReactivate)).Contains(Role.superadmin.ToString()))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن للمسؤول إعادة تفعيل مستخدم ذو صلاحية مشرف عام."
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                var userToReactivate = await _userManager.FindByIdAsync(userId);

                // Representative can only reactivate a family role
                if (userToReactivate != null && !(await _userManager.GetRolesAsync(userToReactivate)).Contains(Role.family.ToString()))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "يمكن للمندوب إعادة تفعيل العائلات فقط."
                    };
                }
            }
            else
            {
                // Other roles are not authorized to reactivate users
                return new ResultResponse
                {
                    Success = false,
                    Message = "ليس لديك صلاحية لإعادة تفعيل المستخدمين."
                };
            }

            // Using transaction for reactivation and audit log
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the user to reactivate
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "المستخدم غير موجود."
                        };
                    }

                    // Perform reactivation by setting isDelete to false
                    user.isDelete = false;
                    var resultUpdate = await _userManager.UpdateAsync(user);
                    if (!resultUpdate.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "حدث خطأ أثناء إعادة تفعيل المستخدم."
                        };
                    }

                    // Create an audit log for the reactivation operation
                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = EntityType.User,
                        RepoId = user.Id.ToString(),
                        Name = AuditName.ReActive,
                        Description = "تم إعادة تفعيل المستخدم.",
                        CreatedDate = DateTime.UtcNow,
                        AdminId = currentUserId
                    };
                    var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                    if (!resultAudit.Success)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = resultAudit.Message,
                        };
                    }

                    // Commit the transaction if everything is successful
                    await transaction.CommitAsync();
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تم إعادة تفعيل المستخدم بنجاح.",
                        result = user
                    };
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = $"حدث خطأ غير متوقع: {ex.Message}"
                    };
                }
            }
        }
        public async Task<List<UserViewModel>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.Include(x => x.Division)
                .ToListAsync();

            var filteredUsers = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(role => role != Role.family.ToString())) // Ensure user has at least one role other than "family"
                {
                    var userRoles = roles
                        .Select(role => Enum.TryParse(role, out Role parsedRole) ? parsedRole : (Role?)null)
                        .Where(role => role != null && role != Role.family) // Exclude family role
                        .ToList()
                        .Select(c => EnumHelper.GetDisplayName(c))
                        .ToList();
                    var divisionName = string.Empty;
                    if (roles.Contains("superadmin") && roles.Count() == 1)
                        divisionName = "مسؤول عام";
                    else
                        divisionName = user.Division.Name;
                    var userViewModel = new UserViewModel();

                    userViewModel.FullName = user.FullName;
                    userViewModel.Id = user.Id;
                    userViewModel.IdNumber = user.IdNumber;
                    userViewModel.IsDelete = user.isDelete;
                    userViewModel.PhoneNumber = user.PhoneNumber;
                    userViewModel.DivisionName = divisionName;
                    userViewModel.CreatedDate = (await _repositoryAudit.GetAllAudit(user.Id, EntityType.User)).ToList().LastOrDefault(x => x.Name == EnumHelper.GetDisplayName(AuditName.Create)).DateCreate;
                    userViewModel.Roles = userRoles;

                    filteredUsers.Add(userViewModel);
                }
            }

            return filteredUsers;
        }


        public async Task<List<UserViewModel>> GetAllRepresentativesAsync(string divisionId, HttpContext httpContext)
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new List<UserViewModel>();

            currentUser = await _context.Users.Include(x => x.Division)
                                              .FirstOrDefaultAsync(x => x.Id == currentUser.Id);
            var currentDivision = string.IsNullOrEmpty(divisionId) ? currentUser.DivisionId.ToString() : divisionId;

            // Get all representatives in the division directly from UserRoles
            var roleName = Role.representative.ToString();
            var users = await (from user in _userManager.Users.Include(x => x.Division)
                               join userRole in _context.UserRoles on user.Id equals userRole.UserId
                               join role in _context.Roles on userRole.RoleId equals role.Id
                               where role.Name == roleName && user.DivisionId.ToString() == currentDivision
                               select user).Distinct().ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            // Fetch all roles in one batch
            var userRoles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .ToListAsync();

            var userTasks = users
                .Where(user => userRoles.Any(r => r.UserId == user.Id && r.Name == Role.representative.ToString()))
                .Select(async user =>
                {
                    var auditLogs = await _repositoryAudit.GetAllAudit(user.Id, EntityType.User);

                    var roles = userRoles
                        .Where(r => r.UserId == user.Id)
                        .Select(r => EnumHelper.GetDisplayName(Enum.Parse<Role>(r.Name)))
                        .ToList();

                    var divisionName = roles.Contains("superadmin") && roles.Count == 1
                        ? "مسؤول عام"
                        : user.Division?.Name ?? "غير معروف";

                    var createdDate = auditLogs
                        .Where(a => a.Name.Equals(EnumHelper.GetDisplayName(AuditName.Create)))
                        .Select(a => a.DateCreate)
                        .LastOrDefault();

                    return new UserViewModel
                    {
                        FullName = user.FullName,
                        Id = user.Id,
                        IdNumber = user.IdNumber,
                        IsDelete = user.isDelete,
                        PhoneNumber = user.PhoneNumber,
                        DivisionName = divisionName,
                        CreatedDate = createdDate,
                        Roles = roles
                    };
                });

            var filteredUsers = await Task.WhenAll(userTasks);
            return filteredUsers.ToList();

        }
        public async Task<List<UserViewModel>> GetAllUsersByDivisionIdAsync(string divisionId)
        {
            // Fetch users from the database, excluding soft-deleted ones
            var users = await _context.Users
                .Where(x => x.DivisionId.ToString().Equals(divisionId))
                .ToListAsync();

            var filteredUsers = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(role => role != Role.family.ToString())) // Ensure user has at least one role other than "family"
                {
                    var userRoles = roles
                        .Select(role => Enum.TryParse(role, out Role parsedRole) ? parsedRole : (Role?)null)
                        .Where(role => role != null && role != Role.family) // Exclude family role
                        .ToList()
                        .Select(c => EnumHelper.GetDisplayName(c))
                        .ToList();
                    var divisionName = string.Empty;
                    if (roles.Contains("superadmin") && roles.Count() == 1)
                        divisionName = "مسؤول عام";
                    else
                        divisionName = user.Division.Name;
                    var userViewModel = new UserViewModel();

                    userViewModel.FullName = user.FullName;
                    userViewModel.Id = user.Id;
                    userViewModel.IdNumber = user.IdNumber;
                    userViewModel.IsDelete = user.isDelete;
                    userViewModel.PhoneNumber = user.PhoneNumber;
                    userViewModel.DivisionName = divisionName;
                    userViewModel.CreatedDate = (await _repositoryAudit.GetAllAudit(user.Id, EntityType.User)).ToList().LastOrDefault(x => x.Name.Equals(EnumHelper.GetDisplayName(AuditName.Create))).DateCreate;
                    userViewModel.Roles = userRoles;

                    filteredUsers.Add(userViewModel);
                }
            }

            return filteredUsers;
        }
        public async Task<ResultResponse> UpdateUserAsync(UserDto userDto, HttpContext httpContext)
        {
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var userToUpdate = await _userManager.FindByIdAsync(userDto.Id);
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "المستخدم غير موجود أو تم حذفه."
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "المستخدم غير موجود أو تم حذفه."
                };
            }

            // Convert userDto role to Enum for easy comparison
            var invalidRoles = userDto.Roles.Where(role => !Enum.IsDefined(typeof(Role), role)).ToList();

            if (invalidRoles.Any())
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"Invalid roles: {string.Join(", ", invalidRoles)}."
                };
            }

            // Proceed with valid roles
            List<Role> validRoles = userDto.Roles.Select(role => (Role)role).ToList();



            Guid? divisionId = new Guid();
            // Role-based permission checks
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can update any user
                if (Guid.TryParse(userDto.DivisionId, out var parsedDivisionId))
                {
                    divisionId = parsedDivisionId;
                }
                else
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "خلل في تحديد الشعبة",
                    };
                }
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin cannot update a Superadmin
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                // Admin cannot add a superadmin
                if (validRoles.Contains(Role.superadmin))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "Admin cannot add a Superadmin user."
                    };
                }
                if (userRoles.Contains(Role.superadmin.ToString()) || !currentUser.DivisionId.ToString().Equals(userDto.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "ليس لديك صلاحية التعديل"
                    };
                }
                divisionId = currentUser.DivisionId;
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                // Representative can only reactivate a family role
                if (userToUpdate != null && currentUserId.Equals(userToUpdate.Id))
                {
                    userToUpdate.PhoneNumber = userDto.PhoneNumber;
                    var resultUpdate = await _userManager.UpdateAsync(userToUpdate);
                    if (!resultUpdate.Succeeded)
                    {
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "حدث خطأ أثناء تحديث بيانات المستخدم."
                        };
                    }
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تم تحديث رقم الهاتف بنجاح",
                        result = userToUpdate
                    };
                }
                // Representative can only reactivate a family role
                if ((userToUpdate != null && !(await _userManager.GetRolesAsync(userToUpdate)).Contains(Role.family.ToString())))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "يمكن للمندوب تعديل العائلات فقط."
                    };
                }
            }
            else
            {
                // Representative can only reactivate a family role
                if (userToUpdate != null && currentUserId.Equals(userToUpdate.Id))
                {
                    userToUpdate.PhoneNumber = userDto.PhoneNumber;
                    var resultUpdate = await _userManager.UpdateAsync(userToUpdate);
                    if (!resultUpdate.Succeeded)
                    {
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "حدث خطأ أثناء تحديث بيانات المستخدم."
                        };
                    }
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تم تحديث رقم الهاتف بنجاح",
                        result = userToUpdate
                    };
                }
                else
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "ليس لديك الصلاحية المناسبة"
                    };
                }
            }

            // Using transaction for updating user and audit log
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    // Update user details
                    userToUpdate.FullName = userDto.FullName;
                    userToUpdate.PhoneNumber = userDto.PhoneNumber;
                    userToUpdate.DivisionId = Guid.TryParse(userDto.DivisionId, out var parsedDivisionId) ? parsedDivisionId : currentUser.DivisionId;
                    userToUpdate.IdNumber = userDto.IdNumber;
                    userToUpdate.UserName = userDto.IdNumber;
                    var resultUpdate = await _userManager.UpdateAsync(userToUpdate);
                    if (!resultUpdate.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "حدث خطأ أثناء تحديث بيانات المستخدم." + resultUpdate.Errors.First().Description
                        };
                    }

                    foreach (Role role in validRoles)
                    {
                        var resultNewRole = await EnsureRoleExistsAsync(role);
                        if (!resultNewRole.Success)
                        {
                            await transaction.RollbackAsync();
                            return new ResultResponse
                            {
                                Success = false,
                                Message = resultNewRole.Message,
                            };
                        }
                        if (!(await _userManager.IsInRoleAsync(userToUpdate, role.ToString())))
                        {
                            var resultUserNewRole = await _userManager.AddToRoleAsync(userToUpdate, role.ToString());

                            if (!resultUserNewRole.Succeeded)
                            {
                                await transaction.RollbackAsync();
                                return new ResultResponse
                                {
                                    Success = false,
                                    Message = " خلل في إضافة الصلاحية للمستخدم",
                                };
                            }
                        }
                    }


                    // Remove all roles that are not in validRoles
                    currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                    validRoles.Add(Role.family);
                    var rolesToRemove = currentRoles.Except(validRoles.Select(role => role.ToString())).ToList();

                    foreach (var roleToRemove in rolesToRemove)
                    {
                        var resultRemoveRole = await _userManager.RemoveFromRoleAsync(userToUpdate, roleToRemove);
                        if (!resultRemoveRole.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            return new ResultResponse
                            {
                                Success = false,
                                Message = "خلل في إزالة الصلاحية للمستخدم",
                            };
                        }
                    }

                    // Audit log creation
                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = EntityType.User,
                        RepoId = userToUpdate.Id.ToString(),
                        Name = AuditName.Update,
                        Description = "تم تحديث بيانات المستخدم.",
                        CreatedDate = DateTime.UtcNow,
                        AdminId = currentUserId
                    };

                    var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                    if (!resultAudit.Success)
                    {
                        await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = resultAudit.Message
                        };
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تم تحديث بيانات المستخدم بنجاح.",
                        result = userToUpdate
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = $"حدث خطأ غير متوقع: {ex.Message}"
                    };
                }
            }
        }

        public async Task<ResultResponse> ResetPasswordUserAsync(string userId, HttpContext httpContext)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // بدء المعاملة
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.isDelete)
                {
                    return new ResultResponse()
                    {
                        Message = " هذا المستخدم غير موجود أو تم حذفه",
                        Success = false
                    };
                }
                var currentUser = await _userManager.GetUserAsync(httpContext.User);
                if (currentUser == null || currentUser.isDelete)
                {
                    return new ResultResponse()
                    {
                        Message = " ليس لديك صلاحية مناسبة",
                        Success = false
                    };
                }

                // تعيين كلمة المرور مساوية لاسم المستخدم
                string newPassword = user.UserName;
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (!resetResult.Succeeded)
                {
                    return new ResultResponse()
                    {
                        Errors = resetResult.Errors.Select(e => e.Description).ToList(),
                        Success = false,
                        Message = "حدث خطأ أثناء إعادة تعيين كلمة المرور"
                    };
                }

                // Audit log creation
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = EntityType.User,
                    RepoId = user.Id.ToString(),
                    Name = AuditName.Update,
                    Description = "تم إعادة تعيين كلمة المرور.",
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id.ToString(),
                };

                var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                if (!resultAudit.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = resultAudit.Message
                    };
                }

                // Commit transaction
                await transaction.CommitAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إعادة تعيين كلمة المرور بنجاح.",
                    result = user
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ غير متوقع: {ex.Message}"
                };
            }
        }


        public async Task<ResultResponse> ResetPasswordUserByFamilyIdAsync(string familyId, HttpContext httpContext)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // بدء المعاملة
            try
            {
                var family = _context.Families.FirstOrDefault(x => x.Id.ToString().Equals(familyId));
                if (family == null || family.IsDeleted)
                {
                    return new ResultResponse()
                    {
                        Message = " هذه العائلة غير موجودة أو تم حذفها",
                        Success = false
                    };
                }
                var user = await _userManager.FindByIdAsync(family.HusbandId);
                if (user == null || user.isDelete)
                {
                    return new ResultResponse()
                    {
                        Message = " هذا المستخدم غير موجود أو تم حذفه",
                        Success = false
                    };
                }
                var currentUser = await _userManager.GetUserAsync(httpContext.User);
                if (currentUser == null || currentUser.isDelete)
                {
                    return new ResultResponse()
                    {
                        Message = " ليس لديك صلاحية مناسبة",
                        Success = false
                    };
                }

                // تعيين كلمة المرور مساوية لاسم المستخدم
                string newPassword = user.UserName;
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (!resetResult.Succeeded)
                {
                    return new ResultResponse()
                    {
                        Errors = resetResult.Errors.Select(e => e.Description).ToList(),
                        Success = false,
                        Message = "حدث خطأ أثناء إعادة تعيين كلمة المرور"
                    };
                }

                // Audit log creation
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = EntityType.User,
                    RepoId = user.Id.ToString(),
                    Name = AuditName.Update,
                    Description = "تم إعادة تعيين كلمة المرور.",
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id.ToString(),
                };

                var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                if (!resultAudit.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = resultAudit.Message
                    };
                }

                // Commit transaction
                await transaction.CommitAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إعادة تعيين كلمة المرور بنجاح.",
                    result = user
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ غير متوقع: {ex.Message}"
                };
            }
        }

        public async Task<ResultResponse> GetUserByContextAsync(HttpContext httpContext)
        {
            var curruntUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            var currentUser = await _userManager.Users.Include(x => x.Division).FirstOrDefaultAsync(x => x.Id.Equals(curruntUserId));
            if (currentUser == null)
                return new ResultResponse()
                {
                    Message = "ليس لديك صلاحية الوصول",
                    Success = false,
                };
            return new ResultResponse()
            {
                Message = "تم جلب المستخدم بنجاح",
                Success = true,
                result = currentUser
            };
        }    }}