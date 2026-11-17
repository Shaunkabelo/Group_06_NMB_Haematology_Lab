using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public interface IDoctorWorkflowRepository
{
    Task<IEnumerable<DoctorRequestRecord>> Requests(int doctorId, int? patientId = null);
    Task<DoctorRequestRecord?> Request(int doctorId, int requestId);
    Task<IEnumerable<DoctorRequestItem>> Items(int doctorId, int requestId);
    Task<IEnumerable<DoctorResultRowDto>> Results(int doctorId, int? requestId = null);
    Task<IEnumerable<DoctorAlertDto>> Alerts(int doctorId, DateTime start, DateTime endExclusive);
    Task<PatientDetailsDto?> Patient(int patientId);
    Task<int> Create(int doctorId, CreateTestRequestModel model, string notification);
    Task<string> Register(PatientRegistrationViewModel model, string hash, string salt, string notification);
    Task<bool> Release(int doctorId, int requestId, string note, string notification);
    Task<bool> EmailResults(int doctorId, int requestId, string notification);
    Task<DoctorEmailJob?> ClaimEmail();
    Task CompleteEmail(DoctorEmailJob job, bool delivered);
    Task<(string Email, string Name, string Reason)?> CancellationRecipient(int requestId);
}
