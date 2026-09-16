using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Microsoft.AspNetCore.Http;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services
{
    public class CurrentTechnicianService : ICurrentTechnicianService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISqlDataAccess _db;

        public CurrentTechnicianService(IHttpContextAccessor httpContextAccessor, ISqlDataAccess db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public async Task<int> GetLabTechnicianIdAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new InvalidOperationException("No authenticated user found.");

            var userId = int.Parse(userIdClaim.Value);

            var result = await _db.GetData<int, object>(
                "usp_Common_GetLabTechnicianIdByUserId", new { UserID = userId });

            var labTechnicianId = result.FirstOrDefault();
            if (labTechnicianId == 0)
                throw new InvalidOperationException("Logged-in user is not a lab technician.");

            return labTechnicianId;
        }
        public async Task<string?> GetEmployeeNumberAsync()
        {
            var userId = GetCurrentUserId();

            var result = await _db.GetData<string, object>(
                "usp_Common_GetEmployeeNumberByUserId", new { UserID = userId });

            return result.FirstOrDefault();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new InvalidOperationException("No authenticated user found.");

            return int.Parse(userIdClaim.Value);
        }
        public Task<int> GetUserIdAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                throw new InvalidOperationException("No authenticated user found.");

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("No authenticated user found.");

            return Task.FromResult(userId);
        }
    }
}
