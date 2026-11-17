using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ITechProfileRepository
    {
        Task<TechProfileModel?> GetProfileAsync(int userId);
        Task<List<AssignedTestTypeModel>> GetAssignedTestTypesAsync(int labTechnicianId);
        Task UpdatePersonalDetailsAsync(int userId, string name, string surname, string? cellphone);
        Task<(string Hash, string Salt)?> GetPasswordHashSaltAsync(int userId);
        Task UpdatePasswordAsync(int userId, string newHash, string newSalt);
        Task UpdateEmailAsync(int userId, string newEmail);
    }
}
