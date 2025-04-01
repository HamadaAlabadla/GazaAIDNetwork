using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using Microsoft.EntityFrameworkCore;

namespace GazaAIDNetwork.Infrastructure.Services.FamilyService
{
    public interface IDiseaseService
    {
        Task<ResultResponse> CreateDiseaseAsync(Disease disease);
        Task<ResultResponse> UpdateDiseaseAsync(Disease disease);
    }
    public class DiseaseService : IDiseaseService
    {
        private readonly ApplicationDbContext _context;
        public DiseaseService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ResultResponse> CreateDiseaseAsync(Disease disease)
        {
            if (disease == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات المرض غير صالحة."
                };
            }

            // Check if the FamilyId exists in the Families table
            var existingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == disease.FamilyId);
            if (existingFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة."
                };
            }

            disease.UpdateIsDelete();

            if (disease.IsDelete)
                return new ResultResponse
                {
                    Success = true,
                    Message = "لا يوجد لدى العائلة أية مرض"
                };
            var existDesease = await _context.Diseases.FirstOrDefaultAsync(d => d.FamilyId == disease.FamilyId && !d.IsDelete);
            if (existDesease != null)
                return await UpdateDiseaseAsync(disease);
            // Add the disease record
            _context.Diseases.Add(disease);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة المرض بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة المرض: {ex.Message}"
                };
            }
        }
        public async Task<ResultResponse> UpdateDiseaseAsync(Disease disease)
        {
            if (disease == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات المرض غير صالحة."
                };
            }

            // Check if the FamilyId exists in the Families table
            var existingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == disease.FamilyId);
            if (existingFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة."
                };
            }

            disease.UpdateIsDelete();

            if (disease.IsDelete)
                return new ResultResponse
                {
                    Success = true,
                    Message = "لا يوجد لدى العائلة أية مرض"
                };
            var existDesease = await _context.Diseases.FirstOrDefaultAsync(d => d.FamilyId == disease.FamilyId && !d.IsDelete);
            if (existDesease == null)
                return new ResultResponse
                {
                    Success = true,
                    Message = "لا يوجد لدى العائلة أية مرض"
                };
            existDesease.Cancer = disease.Cancer;
            existDesease.BloodPressure = disease.BloodPressure;
            existDesease.KidneyFailure = disease.KidneyFailure;
            existDesease.Diabetes = disease.Diabetes;
            // Add the disease record
            _context.Diseases.Update(disease);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة المرض بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة المرض: {ex.Message}"
                };
            }
        }

    }
}
