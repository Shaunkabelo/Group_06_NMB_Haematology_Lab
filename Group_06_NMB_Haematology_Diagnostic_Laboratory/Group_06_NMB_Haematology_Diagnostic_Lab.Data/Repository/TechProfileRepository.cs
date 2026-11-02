using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    internal class PasswordHashSaltRow
    {
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
    }

    public class TechProfileRepository : ITechProfileRepository
    {
        private readonly ISqlDataAccess _db;

        public TechProfileRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<TechProfileModel?> GetProfileAsync(int userId)
        {
            var result = await _db.GetData<TechProfileModel, object>(
                "usp_TechProfile_GetProfile", new { UserID = userId });
            return result.FirstOrDefault();
        }

        public async Task<List<AssignedTestTypeModel>> GetAssignedTestTypesAsync(int labTechnicianId)
        {
            var result = await _db.GetData<AssignedTestTypeModel, object>(
                "usp_TechProfile_GetAssignedTestTypes", new { LabTechnicianID = labTechnicianId });
            return result.ToList();
        }

        public async Task UpdatePersonalDetailsAsync(int userId, string name, string surname, string? cellphone)
        {
            await _db.SaveData(
                "usp_TechProfile_UpdatePersonalDetails",
                new { UserID = userId, Name = name, Surname = surname, Cellphone = cellphone });
        }

        public async Task<(string Hash, string Salt)?> GetPasswordHashSaltAsync(int userId)
        {
            var result = await _db.GetData<PasswordHashSaltRow, object>(
                "usp_TechProfile_GetPasswordHashSalt", new { UserID = userId });
            var row = result.FirstOrDefault();
            return row == null ? null : (row.PasswordHash, row.PasswordSalt);
        }

        public async Task UpdatePasswordAsync(int userId, string newHash, string newSalt)
        {
            await _db.SaveData(
                "usp_TechProfile_UpdatePassword",
                new { UserID = userId, NewPasswordHash = newHash, NewPasswordSalt = newSalt });
        }

        public async Task UpdateEmailAsync(int userId, string newEmail)
        {
            await _db.SaveData(
                "usp_TechProfile_UpdateEmail",
                new { UserID = userId, NewEmail = newEmail });
        }
    }
}
