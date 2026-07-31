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
        Task<IEnumerable<Doctor>> GetDoctors();
        Task CreateDoctor(Doctor doctor);


        Task<Doctor> GetDoctorById(int doctorId);

        Task UpdateDoctor(Doctor doctor);

        Task DeleteDoctor(int doctorId);
    }
}
