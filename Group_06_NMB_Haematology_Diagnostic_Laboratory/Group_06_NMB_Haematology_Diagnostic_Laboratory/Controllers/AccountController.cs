using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public class AccountController : Controller
	{
		private readonly IDoctorRepository _doctorRepository;

		public AccountController(
			IDoctorRepository doctorRepository)
		{
			_doctorRepository = doctorRepository;
		}


		// =========================================================
		// LOGIN PAGE
		// =========================================================

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}


		// =========================================================
		// LOGIN USER
		// =========================================================

		[HttpPost]
		public async Task<IActionResult> Login(
			[FromBody] LoginDto model)
		{
			try
			{
				if (model == null)
				{
					return BadRequest(new
					{
						success = false,
						message = "Login information is required."
					});
				}


				if (string.IsNullOrWhiteSpace(model.Email) ||
					string.IsNullOrWhiteSpace(model.Password))
				{
					return BadRequest(new
					{
						success = false,
						message = "Email and password are required."
					});
				}


				var user =
					await _doctorRepository
						.GetUserByEmailAsync(
							model.Email);


				if (user == null)
				{
					return BadRequest(new
					{
						success = false,
						message = "Invalid email or password."
					});
				}


				if (!user.IsActive)
				{
					return BadRequest(new
					{
						success = false,
						message = "This account is inactive."
					});
				}


				bool passwordCorrect =
					VerifyPassword(
						model.Password,
						user.PasswordHash,
						user.PasswordSalt);


				if (!passwordCorrect)
				{
					return BadRequest(new
					{
						success = false,
						message = "Invalid email or password."
					});
				}


				// =================================================
				// CREATE LOGIN CLAIMS
				// =================================================

				var claims =
					new List<Claim>
					{
						new Claim(
							ClaimTypes.NameIdentifier,
							user.UserId.ToString()
						),

						new Claim(
							ClaimTypes.Name,
							user.Name + " " + user.Surname
						),

						new Claim(
							ClaimTypes.Email,
							user.Email
						),

						new Claim(
							ClaimTypes.Role,
							user.Role
						)
					};


				var identity =
					new ClaimsIdentity(
						claims,
						"MyCookieAuth");


				var principal =
					new ClaimsPrincipal(
						identity);


				await HttpContext.SignInAsync(
					"MyCookieAuth",
					principal);


				// =================================================
				// REDIRECT USER BASED ON ROLE
				// =================================================

				string redirectUrl;


				if (user.Role.Equals(
					"Doctor",
					StringComparison.OrdinalIgnoreCase))
				{
					redirectUrl =
						Url.Action(
							"Dashboard",
							"Doctor")!;
				}
				else
				{
					redirectUrl =
						Url.Action(
							"Login",
							"Account")!;
				}


				return Json(new
				{
					success = true,
					redirectUrl = redirectUrl
				});
			}
			catch (FormatException)
			{
				return StatusCode(
					500,
					new
					{
						success = false,
						message =
							"The password information stored for this account is invalid."
					});
			}
			catch (Exception ex)
			{
				return StatusCode(
					500,
					new
					{
						success = false,
						message = ex.Message
					});
			}
		}


		// =========================================================
		// REGISTRATION PAGE
		// =========================================================

		[HttpGet]
		public IActionResult Registration()
		{
			return View();
		}


		// =========================================================
		// RESET PASSWORD PAGE
		// =========================================================

		[HttpGet]
		public IActionResult ResetPassword()
		{
			return View();
		}


		// =========================================================
		// LOGOUT
		// =========================================================

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(
				"MyCookieAuth");


			return RedirectToAction(
				"Login",
				"Account");
		}


		// =========================================================
		// VERIFY PASSWORD
		// =========================================================

		private bool VerifyPassword(
			string password,
			string storedHash,
			string storedSalt)
		{
			if (string.IsNullOrWhiteSpace(password) ||
				string.IsNullOrWhiteSpace(storedHash) ||
				string.IsNullOrWhiteSpace(storedSalt))
			{
				return false;
			}


			byte[] saltBytes =
				Convert.FromBase64String(
					storedSalt);


			using var pbkdf2 =
				new Rfc2898DeriveBytes(
					password,
					saltBytes,
					100000,
					HashAlgorithmName.SHA256);


			byte[] calculatedHashBytes =
				pbkdf2.GetBytes(32);


			byte[] storedHashBytes =
				Convert.FromBase64String(
					storedHash);


			return CryptographicOperations
				.FixedTimeEquals(
					calculatedHashBytes,
					storedHashBytes);
		}
	}
}