using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

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

        //==========================================================================================
        //Get patient for update
        //==========================================================================================
        public async Task<PatientUpdate> GetPatientForUpdate(
           int patientId)
        {
            var result = await _db.GetData<
                PatientUpdate,
                object>(
                    "dbo.sp_GetPatientForUpdate",
                    new
                    {
                        PatientId = patientId
                    });

            return result.FirstOrDefault();
        }


        //==========================================================================================
        //Update patient information
        //==========================================================================================

        public async Task UpdatePatient(
        PatientUpdate model)
        {
            var parameters = new
            {
                UserId = model.UserId,
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                Cellphone = model.Cellphone,
                HomeAddress = model.HomeAddress,
                DateOfBirth = model.DateOfBirth
            };

            await _db.SaveData(
                "dbo.sp_UpdatePatient",
                parameters);
        }
    }
    }
   

   


