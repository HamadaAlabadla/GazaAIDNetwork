using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using Microsoft.EntityFrameworkCore;

namespace GazaAIDNetwork.Infrastructure.Services.FamilyService
{
    public interface IDisabilityService
    {
        Task<ResultResponse> CreateDisabilityAsync(Disability disability);
        Task<ResultResponse> UpdateDisabilityAsync(Disability disability);
    }
    public class DisabilityService : IDisabilityService
    {
        private readonly ApplicationDbContext _context;
        public DisabilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultResponse> CreateDisabilityAsync(Disability disability)
        {
            if (disability == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات الإعاقة غير صالحة."
                };
            }

            // Check if the FamilyId exists in the Families table
            var existingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == disability.FamilyId);
            if (existingFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة."
                };
            }
            disability.UpdateIsDelete();

            if (disability.IsDelete)
                return new ResultResponse
                {
                    Success = true,
                    Message = "لا يوجد لدى العائلة أية إعاقة"
                };
            var existDisability = _context.Disabilities.FirstOrDefault(f => f.FamilyId == disability.FamilyId && !f.IsDelete);
            if (existDisability != null)
                return await UpdateDisabilityAsync(disability);
            // Add the disability record
            _context.Disabilities.Add(disability);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة بيانات الإعاقة بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة بيانات الإعاقة: {ex.Message}"
                };
            }
        }
        public async Task<ResultResponse> UpdateDisabilityAsync(Disability disability)
        {
            if (disability == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات الإعاقة غير صالحة."
                };
            }

            // Check if the FamilyId exists in the Families table
            var existingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == disability.FamilyId);
            if (existingFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة."
                };
            }
            disability.UpdateIsDelete();

            if (disability.IsDelete)
                return new ResultResponse
                {
                    Success = true,
                    Message = "لا يوجد لدى العائلة أية إعاقة"
                };

            var existDisability = _context.Disabilities.FirstOrDefault(f => f.FamilyId == disability.FamilyId && !f.IsDelete);
            if (existDisability == null)
                return new ResultResponse
                {
                    Success = false,
                    Message = "لا يوجد لدى العائلة أية إعاقة"
                };

            existDisability.Motor = disability.Motor;
            existDisability.Hearing = disability.Hearing;
            existDisability.Mental = disability.Mental;
            existDisability.Visual = disability.Visual;
            // Add the disability record
            _context.Disabilities.Update(disability);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة بيانات الإعاقة بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة بيانات الإعاقة: {ex.Message}"
                };
            }
        }

    }
}
