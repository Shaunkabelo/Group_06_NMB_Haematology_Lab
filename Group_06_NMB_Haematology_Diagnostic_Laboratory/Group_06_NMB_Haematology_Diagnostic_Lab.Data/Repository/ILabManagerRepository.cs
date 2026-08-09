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
        Task<IEnumerable<Doctor>> GetDoctors(string filter = "Active");
        Task CreateDoctor(Doctor doctor);
        Task<Doctor> GetDoctorById(int doctorId);
        Task UpdateDoctor(Doctor doctor);
        Task DeleteDoctor(int doctorId);
        Task ReactivateDoctor(int doctorId);

        //      ==== TECHNICIAN ===
        Task<IEnumerable<LabTechnician>> GetTechnicians(string filter = "Active");

        Task<LabTechnician?> GetTechnicianById(int technicianId);

        Task CreateTechnician(LabTechnician technician);

        Task UpdateTechnician(LabTechnician technician);

        Task DeleteTechnician(int technicianId);
        Task ReactivateTechnician(int technicianId);

        //      === LAB MANAGER ===
        Task<IEnumerable<LabManager>> GetManagers(string filter = "Active");

        Task<LabManager?> GetManagerById(int managerId);

        Task CreateManager(LabManager manager);

        Task UpdateManager(LabManager manager);

        Task DeleteManager(int managerId);
        Task ReactivateManager(int managerId);

        // ============================ SUPPLIER ============================

        Task<IEnumerable<Supplier>> GetSuppliers();

        Task<Supplier?> GetSupplierById(int supplierId);

        Task CreateSupplier(Supplier supplier);

        Task UpdateSupplier(Supplier supplier);

        Task DeleteSupplier(int supplierId);
    }

}
