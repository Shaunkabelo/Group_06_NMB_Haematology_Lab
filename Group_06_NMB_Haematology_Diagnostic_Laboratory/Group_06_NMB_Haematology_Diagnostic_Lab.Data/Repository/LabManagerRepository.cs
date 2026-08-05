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

        // ============================== DOCTOR ================================================================
        //=======================================================================================================

        // ------------------- GET/DISPLAY DOCTORS -----------
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
        // -------------- CREATE/ADD DOCTORS ----------------
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
        //-------------------------- DET DOCTOR BY ID [For Delete & Update]
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
        // ---------------------- UPDATE DOCTOR ----------------------
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
        // --------------DELETE DOCTOR------[Soft delete]
        public async Task DeleteDoctor(int doctorId)
        {
            await _db.SaveData(
                "sp_Doctor_Delete",
                new
                {
                    DoctorID = doctorId
                });
        }
        //===========================================================================================================
        //===========================================================================================================





        // ============================TECHNICIAN ===============================================================
        //=======================================================================================================

        //------------------------- DISPLAY TECHNICIANS
        public async Task<IEnumerable<LabTechnician>> GetTechnicians() 
        {
            return await _db.GetData<LabTechnician, User, LabTechnician, dynamic>(
                "sp_LabTechnician_GetAll",
                (technician, user) =>
                {
                    technician.User = user;
                    return technician;
                },
                new { },
                splitOn: "UserId"
            );
        }

        // ----------------SELECT TECH BY ID [For Delete & Update]------------------
        public async Task<LabTechnician?> GetTechnicianById(int technicianId)
        {
            var technician = await _db.GetData<LabTechnician, User, LabTechnician, dynamic>(
                "sp_LabTechnician_GetById",
                (technician, user) =>
                {
                    technician.User = user;
                    return technician;
                },
                new
                {
                    LabTechnicianID = technicianId
                },
                splitOn: "UserId"
            );

            return technician.FirstOrDefault();
        }

        // ------------------------   CREATE / ADD TECHNICIAN --------------------------------
        public async Task CreateTechnician(LabTechnician technician)
        {
            string password = PasswordHelper.GenerateTemporaryPassword();

            string salt = PasswordHelper.GenerateSalt();

            string hash = PasswordHelper.HashPassword(password, salt);

            await _db.SaveData(
                "sp_LabTechnician_Create",
                new
                {
                    technician.User.Name,
                    technician.User.Surname,
                    technician.User.Email,

                    PasswordHash = hash,
                    PasswordSalt = salt,

                    technician.User.Cellphone,
                    technician.User.HomeAddress,

                    SA_ID_Number = technician.User.SaIdNumber,

                    technician.User.DateOfBirth,
                    
                });
        }

        //  ---------------------------- UPDATE TECHNICIAN -----------------------------------
        public async Task UpdateTechnician(LabTechnician technician)
        {
            await _db.SaveData(
                "sp_LabTechnician_Update",
                new
                {
                    technician.LabTechnicianId,

                    technician.User.Name,
                    technician.User.Surname,
                    technician.User.Email,
                    technician.User.Cellphone,
                    technician.User.HomeAddress,

                    SA_ID_Number = technician.User.SaIdNumber,

                    technician.User.DateOfBirth,
                    
                    
                });
        }
        // ------------------------ DELETE TECHNICIAN [Soft Delete] ------------
        public async Task DeleteTechnician(int technicianId)
        {
            await _db.SaveData(
                "sp_LabTechnician_Delete",
                new
                {
                    LabTechnicianID = technicianId
                });
        }
        //==================================================================================================


        //================================ LAB MANAGER =====================================================
        //==================================================================================================


        // ------------------------- DISPLAY MANAGERS -------------------------
        public async Task<IEnumerable<LabManager>> GetManagers()
        {
            return await _db.GetData<LabManager, User, LabManager, dynamic>(
                "sp_LabManager_GetAll",
                (manager, user) =>
                {
                    manager.User = user;
                    return manager;
                },
                new { },
                splitOn: "UserId"
            );
        }

        // ------------------------- GET MANAGER BY ID -------------------------
        public async Task<LabManager?> GetManagerById(int managerId)
        {
            var manager = await _db.GetData<LabManager, User, LabManager, dynamic>(
                "sp_LabManager_GetById",
                (manager, user) =>
                {
                    manager.User = user;
                    return manager;
                },
                new
                {
                    LabManagerID = managerId
                },
                splitOn: "UserId"
            );

            return manager.FirstOrDefault();
        }

        // ------------------------- CREATE MANAGER -------------------------
        public async Task CreateManager(LabManager manager)
        {
            string password = PasswordHelper.GenerateTemporaryPassword();

            string salt = PasswordHelper.GenerateSalt();

            string hash = PasswordHelper.HashPassword(password, salt);

            await _db.SaveData(
                "sp_LabManager_Create",
                new
                {
                    manager.User.Name,
                    manager.User.Surname,
                    manager.User.Email,

                    PasswordHash = hash,
                    PasswordSalt = salt,

                    manager.User.Cellphone,
                    manager.User.HomeAddress,

                    SA_ID_Number = manager.User.SaIdNumber,

                    manager.User.DateOfBirth
                });
        }

        // ------------------------- UPDATE MANAGER -------------------------
        public async Task UpdateManager(LabManager manager)
        {
            await _db.SaveData(
                "sp_LabManager_Update",
                new
                {
                    manager.LabManagerId,

                    manager.User.Name,
                    manager.User.Surname,
                    manager.User.Email,
                    manager.User.Cellphone,
                    manager.User.HomeAddress,

                    SA_ID_Number = manager.User.SaIdNumber,

                    manager.User.DateOfBirth
                });
        }

        // ------------------------- DELETE MANAGER -------------------------
        public async Task DeleteManager(int managerId)
        {
            await _db.SaveData(
                "sp_LabManager_Delete",
                new
                {
                    LabManagerID = managerId
                });
        }
    }
}
