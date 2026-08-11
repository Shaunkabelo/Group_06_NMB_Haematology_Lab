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

        Task<IEnumerable<Supplier>> GetSuppliers(string filter = "Active", string search = "");

        Task<Supplier?> GetSupplierById(int supplierId);

        Task CreateSupplier(Supplier supplier);

        Task UpdateSupplier(Supplier supplier);

        Task DeleteSupplier(int supplierId);
        Task ReactivateSupplier(int supplierId);


        // ============================ CONSUMABLES ============================

        Task<IEnumerable<Consumable>> GetConsumables(
        string filter = "Active",
        string search = "");

        Task<Consumable?> GetConsumableById(int consumableId);

        Task CreateConsumable(Consumable consumable,int? performedByUserId = null);

        Task UpdateConsumable(Consumable consumable);

        Task DeleteConsumable(int consumableId);

        Task ReactivateConsumable(int consumableId);

        Task UpdateConsumableStock(int consumableId,int changeAmount,string reason,int? referenceId = null,
            int? performedByUserId = null);


        // ==================== TEST TYPES ====================

        Task<IEnumerable<TestType>> GetTestTypes(
        string filter = "Active",
        string search = "");

        Task<TestType> GetTestTypeById(int id);

        Task CreateTestType(TestType testType);

        Task UpdateTestType(TestType testType);

        Task DeleteTestType(int id);

        Task ReactivateTestType(int id);


        // ==================== TEST TYPE CONSUMABLES ====================

        //Task<IEnumerable<TestTypeConsumable>> GetTestTypeConsumables(int testTypeId);

        //Task AddTestTypeConsumable(TestTypeConsumable testTypeConsumable);

        //Task DeleteTestTypeConsumable(int testTypeConsumableId);

        //Task DeleteTestTypeConsumables(int testTypeId);

        // ==================== TEST CATEGORIES ====================

        Task<IEnumerable<TestCategory>> GetTestCategories(
            string filter = "Active",
            string search = "");

        Task<TestCategory> GetTestCategoryById(int id);

        Task CreateTestCategory(TestCategory category);

        Task UpdateTestCategory(TestCategory category);

        Task DeleteTestCategory(int id);

        Task ReactivateTestCategory(int id);



        // ==================== SAMPLE TYPES ====================

        // ==================== SAMPLE TYPES ====================

        Task<IEnumerable<SampleType>> GetSampleTypes(
            string filter = "Active",
            string search = "");

        Task<SampleType> GetSampleTypeById(int id);

        Task CreateSampleType(SampleType sampleType);

        Task UpdateSampleType(SampleType sampleType);

        Task DeleteSampleType(int id);

        Task ReactivateSampleType(int id);
    }
}
