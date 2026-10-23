using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;

public partial class DoctorController
{
    private static DoctorReportDto ToReport(DoctorRequestRecord r) => new()
    {
        TestRequestId = r.Id, PatientName = r.PatientName, DoctorName = r.DoctorName, PatientSaIdNumber = r.SaIdNumber,
        RequestDate = r.CreatedAt, Status = r.Status, Urgency = r.Urgency, Tests = r.TestNames
    };

    private static object RequestView(DoctorRequestRecord r) => new
    {
        r.Id, r.PatientId, r.PatientName, r.SaIdNumber, r.CreatedAt,
        status = r.Status.Equals("Sample(s) received", StringComparison.OrdinalIgnoreCase)
            ? "Samples Received" : r.Status,
        r.Urgency,
        r.ClinicalNotes, r.CancellationReason, r.CancelledBy, r.CancelledDate,
        r.ReleaseDate, r.DoctorReleaseNote, r.ConsentGranted,
        tests = new[] { r.TestNames }
    };

    [HttpGet]
    public async Task<IActionResult> GetRequests()
    {
        var doctorId = await GetLoggedInDoctorIdAsync();
        if (!doctorId.HasValue) return Forbid();
        return Json((await _workflow.Requests(doctorId.Value)).Select(RequestView));
    }

    [HttpGet]
    public async Task<IActionResult> GetPatientHistory(int patientId)
    {
        if (patientId <= 0) return BadRequest();
        var doctorId = await GetLoggedInDoctorIdAsync();
        if (!doctorId.HasValue) return Forbid();
        return Json((await _workflow.Requests(doctorId.Value, patientId)).Select(RequestView));
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestData(int id)
    {
        var doctorId = await GetLoggedInDoctorIdAsync();
        if (!doctorId.HasValue) return Forbid();
        var request = await _workflow.Request(doctorId.Value, id);
        if (request == null) return NotFound();
        return Json(new { request = RequestView(request),
            items = await _workflow.Items(doctorId.Value, id),
            results = await _workflow.Results(doctorId.Value, id) });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResultPdf(int id)
    {
        var doctorId = await GetLoggedInDoctorIdAsync();
        if (!doctorId.HasValue) return Forbid();
        var request = await _workflow.Request(doctorId.Value, id);
        if (request == null) return NotFound();
        var results = (await _workflow.Results(doctorId.Value, id)).ToList();
        if (results.Count == 0) return NotFound();
        return File(DoctorRequestsPdfReport.BuildResults(request, results), "application/pdf", $"test-results-{id}.pdf");
    }

    [HttpPost, EnableRateLimiting("account")]
    public Task<IActionResult> ReleaseResults([FromBody] DoctorResultMessage input) => SendResults(input, true);

    [HttpPost, EnableRateLimiting("account")]
    public Task<IActionResult> EmailResults([FromBody] DoctorResultMessage input) => SendResults(input, false);

    private async Task<IActionResult> SendResults(DoctorResultMessage input, bool release)
    {
        if (input == null || !ModelState.IsValid || input.TestRequestId <= 0 ||
            string.IsNullOrWhiteSpace(input.Note) || input.Note.Length > 2000 ||
            input.Delivery is not ("Results" or "Appointment"))
            return BadRequest(new { message = "Enter a note (up to 2000 characters) and choose results or appointment email." });
        var doctorId = await GetLoggedInDoctorIdAsync();
        if (!doctorId.HasValue) return Forbid();
        var request = await _workflow.Request(doctorId.Value, input.TestRequestId);
        if (request == null || request.DoctorId != doctorId.Value) return NotFound();
        var subject = input.Delivery == "Results" ? "Your laboratory results" : "Please arrange an appointment";
        var body = input.Delivery == "Results"
            ? input.Note.Trim()
            : $"Laboratory request #{request.Id}\n\n{input.Note.Trim()}\n\nPlease contact your doctor to arrange a discussion of your results.";
        var saved = release
            ? await _workflow.Release(doctorId.Value, request.Id, input.Note.Trim())
            : await _workflow.EmailResults(doctorId.Value, request.Id, input.Delivery == "Appointment");
        if (!saved) return Conflict(new { message = release
            ? "Release requires a completed request with every test independently verified. It may already be released."
            : input.Delivery == "Appointment"
                ? "An appointment invitation requires a completed request with every test independently verified."
                : "Email requires a released request with verified results." });

        byte[]? attachment = null;
        if (input.Delivery == "Results")
        {
            var results = (await _workflow.Results(doctorId.Value, request.Id)).ToList();
            if (results.Count == 0 || results.Any(r => r.VerificationStatus != "Verified"))
                return Conflict(new { message = release
                    ? "Results were released, but the verified report could not be prepared."
                    : "The verified report could not be prepared for email." });
            attachment = DoctorRequestsPdfReport.BuildResults(request, results);
        }

        try
        {
            await _doctorEmailSender.Send(new DoctorEmail(request.Email, subject, body,
                doctorId.Value, AttachResults: input.Delivery == "Results"), attachment, HttpContext.RequestAborted);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = release
                ? "Results were released, but the email could not be sent. Use Email results to retry after checking SMTP settings."
                : "The email could not be sent. Check the SMTP settings and try again." });
        }

        try
        {
            await _workflow.RecordDirectEmail(doctorId.Value, request.Id,
                input.Delivery == "Appointment" ? "AppointmentEmail" : "ResultEmail");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message =
                "The email was sent, but its delivery status could not be saved. Check the Results page before retrying." });
        }

        return Ok(new { success = true, message = release
            ? "Results released and email sent."
            : input.Delivery == "Appointment"
                ? "Appointment invitation sent."
                : "Patient email sent." });
    }
}
