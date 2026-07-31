using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class LabManagerRepository : ILabManagerRepository
    {
        private readonly ISqlDataAccess _db;

        public LabManagerRepository(ISqlDataAccess db)
        {
            _db = db;
        }
        public async Task<IEnumerable<Doctor>> GetDoctors()
        {
            return await _db.GetData<Doctor, User, Doctor, dynamic>(
                "sp_Doctor_GetAll",
                (doctor, user) =>
                {
                    doctor.User = user;
                    return doctor;
                },
                new { },
                splitOn: "UserId"
            );
        }

        
        public async Task CreateDoctor(Doctor doctor)
        {
            string password = PasswordHelper.GenerateTemporaryPassword();

            string salt = PasswordHelper.GenerateSalt();

            string hash = PasswordHelper.HashPassword(password, salt);

            await _db.SaveData(
                "sp_Doctor_Create",
                new
                {
                    doctor.User.Name,
                    doctor.User.Surname,
                    doctor.User.Email,

                    PasswordHash = hash,
                    PasswordSalt = salt,

                    doctor.User.Cellphone,
                    doctor.User.HomeAddress,

                    SA_ID_Number = doctor.User.SaIdNumber,

                    doctor.User.DateOfBirth,

                    HPCSA_Number = doctor.HpcsaNumber
                });
        }

        public async Task<Doctor?> GetDoctorById(int doctorId)
        {
            var doctor = await _db.GetData<Doctor, User, Doctor, dynamic>(
                "sp_Doctor_GetById",
                (doctor, user) =>
                {
                    doctor.User = user;
                    return doctor;
                },
                new
                {
                    DoctorID = doctorId
                },
                splitOn: "UserID"
            );

            return doctor.FirstOrDefault();
        }

        public async Task UpdateDoctor(Doctor doctor)
        {
            await _db.SaveData(
                "sp_Doctor_Update",
                new
                {
                    doctor.DoctorId,

                    doctor.User.Name,
                    doctor.User.Surname,
                    doctor.User.Email,
                    doctor.User.Cellphone,
                    doctor.User.HomeAddress,

                    SA_ID_Number = doctor.User.SaIdNumber,

                    doctor.User.DateOfBirth,

                    HPCSA_Number = doctor.HpcsaNumber
                });
        }

        public async Task DeleteDoctor(int doctorId)
        {
            await _db.SaveData(
                "sp_Doctor_Delete",
                new
                {
                    DoctorID = doctorId
                });
        }

    }
}
