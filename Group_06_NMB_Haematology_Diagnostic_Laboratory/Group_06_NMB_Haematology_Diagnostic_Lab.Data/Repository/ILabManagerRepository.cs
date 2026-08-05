using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public interface ILabManagerRepository
    {
        //      ==== DOCTOR ====
        Task<IEnumerable<Doctor>> GetDoctors();
        Task CreateDoctor(Doctor doctor);
        Task<Doctor> GetDoctorById(int doctorId);
        Task UpdateDoctor(Doctor doctor);
        Task DeleteDoctor(int doctorId);

        //      ==== TECHNICIAN ===
        Task<IEnumerable<LabTechnician>> GetTechnicians();

        Task<LabTechnician?> GetTechnicianById(int technicianId);

        Task CreateTechnician(LabTechnician technician);

        Task UpdateTechnician(LabTechnician technician);

        Task DeleteTechnician(int technicianId);

        //      === LAB TECHNICIAN ===
        Task<IEnumerable<LabManager>> GetManagers();

        Task<LabManager?> GetManagerById(int managerId);

        Task CreateManager(LabManager manager);

        Task UpdateManager(LabManager manager);

        Task DeleteManager(int managerId);
    }

}
