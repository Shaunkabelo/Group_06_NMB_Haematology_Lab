using System.Net;
using System.Net.Sockets;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

var details = new List<ConsentEmailDetail> {
    new() { DoctorEmail = "doctor@example.test", DoctorName = "Ada Smith", PatientName = "Jane Patient", TestName = "Haemoglobin", RequestNumber = "REQ-2026-09975", GrantedDate = new DateTime(2026, 10, 11) },
    new() { DoctorEmail = "doctor@example.test", DoctorName = "Ada Smith", PatientName = "Jane Patient", TestName = "Platelet Count", RequestNumber = "REQ-2026-09975", GrantedDate = new DateTime(2026, 10, 11) },
    new() { DoctorEmail = "doctor@example.test", DoctorName = "Ada Smith", PatientName = "Jane Patient", TestName = "WBC Count", RequestNumber = "REQ-2026-09976", GrantedDate = new DateTime(2026, 10, 12) }
};
var body = ConsentEmailService.CreateBody(details);
foreach (var expected in new[] { "Ada Smith", "Jane Patient", "Haemoglobin", "Platelet Count", "WBC Count", "REQ-2026-09975", "REQ-2026-09976", "11 October 2026", "12 October 2026" })
    if (!body.Contains(expected)) throw new Exception("Missing email detail: " + expected);
try { await new ConsentEmailService(new ConfigurationBuilder().Build()).SendGrantedAccessAsync(details); throw new Exception("Missing configuration was accepted"); }
catch (InvalidOperationException) { }
var revokedAt = new DateTime(2026, 10, 13);
var revokedBody = ConsentEmailService.CreateBody(details.Take(1).ToList(), revokedAt);
foreach (var expected in new[] { "Jane Patient", "has revoked your access", "Haemoglobin", "REQ-2026-09975", "13 October 2026" })
    if (!revokedBody.Contains(expected)) throw new Exception("Missing revocation detail: " + expected);
if (revokedBody.Contains("Platelet Count") || revokedBody.Contains("WBC Count") || revokedBody.Contains("Access granted:"))
    throw new Exception("Revocation included unaffected tests or grant wording");
try {
    ConsentEmailService.CreateBody(new List<ConsentEmailDetail> { details[0], new() { DoctorEmail = "another@example.test" } });
    throw new Exception("Mixed recipients accepted");
} catch (ArgumentException) { }
async Task CheckSmtp(bool revoke, bool registration = false) {
using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
var commands = new List<string>();
var message = new StringBuilder();
var server = Task.Run(async () => {
    using var client = await listener.AcceptTcpClientAsync();
    using var stream = client.GetStream();
    using var reader = new StreamReader(stream);
    using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
    await writer.WriteLineAsync("220 localhost test SMTP");
    bool data = false;
    while (await reader.ReadLineAsync() is { } line) {
        if (data) {
            if (line == ".") { data = false; await writer.WriteLineAsync("250 accepted"); }
            else message.AppendLine(line);
            continue;
        }
        commands.Add(line);
        if (line.StartsWith("DATA")) { data = true; await writer.WriteLineAsync("354 send message"); }
        else if (line.StartsWith("QUIT")) { await writer.WriteLineAsync("221 goodbye"); break; }
        else await writer.WriteLineAsync("250 OK");
    }
});
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
    ["Email:Smtp:Host"] = "127.0.0.1", ["Email:Smtp:Port"] = port.ToString(),
    ["Email:Smtp:EnableSsl"] = "false", ["Email:Smtp:FromAddress"] = "lab@example.test"
}).Build();
if (registration) await new RegistrationEmailService(config).SendAccountCreatedAsync("patient@example.test", "Jane", "Patient", new DateTime(2026, 10, 11, 10, 30, 0)); else if (revoke) await new ConsentEmailService(config).SendRevokedAccessAsync(details.Take(1).ToList(), revokedAt); else await new ConsentEmailService(config).SendGrantedAccessAsync(details);
await server.WaitAsync(TimeSpan.FromSeconds(10));
if (!commands.Any(command => command.Contains(registration ? "RCPT TO:<patient@example.test>" : "RCPT TO:<doctor@example.test>", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Incorrect SMTP recipient");
if (commands.Count(command => command.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase)) != 1) throw new Exception("More than one recipient");
if (!message.ToString().Contains(registration ? "Patient account created successfully" : revoke ? "Patient revoked access to test results" : "Patient granted access to test results")) throw new Exception("Missing email subject");
Console.WriteLine("PASS: patient and doctor names, selected tests, request numbers, grant dates, missing configuration, and SMTP delivery to a local test receiver.");

}
await CheckSmtp(false);
await CheckSmtp(true);
Console.WriteLine("PASS: revocation includes only selected tests and both notifications target exactly one doctor.");

var registrationBody = RegistrationEmailService.CreateBody("patient@example.test", "Jane", "Patient", new DateTime(2026, 10, 11, 10, 30, 0));
foreach (var expected in new[] { "Dear Jane Patient", "created successfully", "Username: patient@example.test", "11 October 2026 12:30 SAST" })
    if (!registrationBody.Contains(expected)) throw new Exception("Missing registration detail: " + expected);
await CheckSmtp(false, true);
Console.WriteLine("PASS: registration confirmation content and SMTP delivery only to the new patient.");