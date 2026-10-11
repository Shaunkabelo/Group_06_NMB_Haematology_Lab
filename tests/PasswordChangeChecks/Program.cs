using System.Reflection;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

var passwordService = new PasswordService();
foreach (bool admin in new[] { true, false })
{
    var db = DispatchProxy.Create<ISqlDataAccess, DatabaseStub>();
    var stub = (DatabaseStub)(object)db;
    var old = passwordService.HashPassword("OldPassword!2");
    stub.Credential = new PasswordCredential { Email = "signed-in@example.test", PasswordHash = old.Hash, PasswordSalt = old.Salt };
    var repo = new PatientRepository(db, passwordService);
    var input = new AdminPasswordChange { Email = "someone-else@example.test", CurrentPassword = "OldPassword!2", NewPassword = "NewPassword!3", ConfirmPassword = "NewPassword!3" };
    Task Change() => admin ? repo.ChangeAdminPassword(7, input) : repo.ChangePatientPassword(7, input);
    await Change();
    var procedureArgs = stub.ProcedureArguments!;
    string Value(string name) => (string)procedureArgs.GetType().GetProperty(name)!.GetValue(procedureArgs)!;
    if (stub.Procedure != "dbo.sp_ChangeUserPassword" || Value("Email") != stub.Credential.Email) throw new Exception("Wrong procedure or account");
    if (Value("CurrentPasswordHash") != old.Hash || Value("CurrentPasswordSalt") != old.Salt) throw new Exception("Incorrect current credentials");
    if (!passwordService.VerifyPassword(input.NewPassword, Value("NewPasswordHash"), Value("NewPasswordSalt"))) throw new Exception("New hash cannot be verified");
    if (Value("NewPasswordSalt") == old.Salt || Value("ConfirmPassword") != input.NewPassword) throw new Exception("Salt/confirmation invalid");
    if (!stub.Query.Contains(admin ? "@AdminID" : "@PatientID") || !stub.Query.Contains("IsActive = 1")) throw new Exception("Missing account scoping");
    foreach (var scenario in new[] { "wrong-current", "mismatch", "weak", "too-long", "missing-account" })
    {
        input.CurrentPassword = scenario == "wrong-current" ? "WrongPassword!1" : "OldPassword!2";
        input.NewPassword = scenario == "weak" ? "weak" : scenario == "too-long" ? "Password!1" + new string('a', 256) : "NewPassword!3";
        input.ConfirmPassword = scenario == "mismatch" ? "Different!1" : input.NewPassword;
        if (scenario == "missing-account") stub.Credential = null;
        var calls = stub.Calls;
        try { await Change(); throw new Exception("Invalid password accepted: " + scenario); }
        catch (InvalidOperationException) { }
        if (stub.Calls != calls) throw new Exception("Invalid credentials reached update procedure");
    }
    Console.WriteLine($"PASS: {(admin ? "Admin" : "Patient")} account scoping, current password, new hash, procedure arguments and validation.");
}

public class DatabaseStub : DispatchProxy
{
    public PasswordCredential? Credential;
    public string Query = "", Procedure = "";
    public object? ProcedureArguments;
    public int Calls;
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "QueryFirstOrDefaultAsync") { Query = (string)args![0]!; return Task.FromResult(Credential); }
        if (method.Name == "SaveDataReturnId") { Calls++; Procedure = (string)args![0]!; ProcedureArguments = args[1]; return Task.FromResult(new PasswordChangeResult { Success = 1 }); }
        throw new Exception("Unexpected data access: " + method.Name);
    }
}
