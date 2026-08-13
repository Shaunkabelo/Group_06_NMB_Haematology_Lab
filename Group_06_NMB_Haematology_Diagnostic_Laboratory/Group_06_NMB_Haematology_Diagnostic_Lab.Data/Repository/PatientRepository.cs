using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
        public class PatientRepository : IPatientRepository
        {
            private readonly ISqlDataAccess _db;

        private readonly IPasswordService _pass;

        public PatientRepository(ISqlDataAccess db, IPasswordService pass)
        {
                    _db = db;
            
    
                _db = db;
                _pass = pass;
            }
        

            public async Task<PatientRegistrationResult> RegisterPatient(
                PatientRegistrationViewModel model)
            {
            var passwordResult =
        _pass.HashPassword(model.Password);

            var parameters = new
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    SaIdNumber = model.SaIdNumber,

                    DateOfBirth = model.DateOfBirth.HasValue
            ? model.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
            : (DateTime?)null,

                    Cellphone = model.Cellphone,
                    Email = model.Email,
                    HomeAddress = model.HomeAddress,

                     PasswordHash = passwordResult.Hash,
                    PasswordSalt = passwordResult.Salt
                };

                return await _db.SaveDataReturnId<
                    PatientRegistrationResult,
                    object>(
                        "sp_RegisterPatient",
                        parameters);
            }

            public Task RegisterPatient(User user)
            {
                throw new NotImplementedException();
            }
        }
    }

   


