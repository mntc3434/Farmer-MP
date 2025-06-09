using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModel;
using Farmer_MP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        #region Login/Logout

        [HttpGet]
        
        public IActionResult Login(string returnUrl = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogDebug("Login GET - UserId in session: {UserId}", userId);

            if (userId != null)
            {
                _logger.LogDebug("Redirecting to dashboard");
                return RedirectToDashboard();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation("Attempting login for email: {Email}", model.Email);

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
                }

                _logger.LogDebug("User found: {UserId}, EmailVerified: {IsVerified}", user.UserID, user.IsEmailVerified);

                if (!VerifyPassword(model.Password, user.Password))
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.UserID);
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
                }

                if (!user.IsEmailVerified)
                {
                    _logger.LogWarning("Login failed: Email not verified for user {UserId}", user.UserID);
                    TempData["UnverifiedEmail"] = user.Email; // Store for resend link
                    ModelState.AddModelError(string.Empty, "Please verify your email first.");
                    return View(model);
                }

                // Create session
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserType", user.UserType);
                HttpContext.Session.SetString("UserName", user.Name);

                _logger.LogInformation("Login successful for user {UserId}", user.UserID);

                return RedirectToLocal(returnUrl) ?? RedirectToDashboard(user.UserType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            HttpContext.Session.Clear();
            _logger.LogInformation("User {UserID} logged out", userId);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        #endregion

        #region Registration & Verification

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Generate OTP
                var otp = GenerateOTP();

                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = HashPassword(model.Password),
                    Phone = model.Phone.Replace(" ", ""), // Remove spaces from phone
                    Address = model.Address,
                    UserType = model.UserType,
                    CreatedAt = DateTime.UtcNow,
                    IsEmailVerified = false,
                    OTP = otp,
                    OTPExpiry = DateTime.UtcNow.AddMinutes(10)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send verification email
                try
                {
                    var verificationLink = Url.Action(
                        "VerifyEmail",
                        "Account",
                        new { email = user.Email, otp },
                        protocol: Request.Scheme);

                    var emailSubject = "Verify Your AgroMarket Account";
                    var emailBody = $@"
                <h2>Welcome to AgroMarket, {user.Name}!</h2>
                <p>Thank you for registering. Please verify your email address to activate your account.</p>
                <p>Your verification code is: <strong>{otp}</strong></p>
                <p>Or click this link to verify: <a href='{verificationLink}'>Verify My Email</a></p>
                <p><em>This code will expire in 10 minutes.</em></p>";

                    await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

                    TempData["EmailForVerification"] = user.Email;
                    return RedirectToAction("VerifyEmail", new { email = user.Email });
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send verification email");

                    // Remove the user if email fails to send
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = "Failed to send verification email. Please try again.";
                    return View(model);
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during registration");
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email address is required for verification.";
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string email, string otp)
        {
            try
            {
                _logger.LogInformation("Starting verification for email: {Email}", email);

                // Validate inputs
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
                {
                    TempData["ErrorMessage"] = "Both email and OTP are required.";
                    return RedirectToAction("VerifyEmail", new { email });
                }

                // Find user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please register again.";
                    return RedirectToAction("Register");
                }

                _logger.LogDebug("User found: {UserID}, Verified: {IsVerified}", user.UserID, user.IsEmailVerified);

                // Check if already verified
                if (user.IsEmailVerified)
                {
                    TempData["SuccessMessage"] = "Email already verified. Please log in.";
                    return RedirectToAction("Login");
                }

                // Verify OTP
                if (string.IsNullOrEmpty(user.OTP) || user.OTP != otp)
                {
                    TempData["ErrorMessage"] = "Invalid OTP. Please try again.";
                    return View();
                }

                if (user.OTPExpiry < DateTime.UtcNow)
                {
                    TempData["ErrorMessage"] = "OTP has expired. Please request a new one.";
                    return View();
                }

                // Update user
                user.IsEmailVerified = true;
                user.OTP = null;
                user.OTPExpiry = null;

                // Save changes with explicit error handling
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User {UserId} verified successfully", user.UserID);
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error during verification");
                    TempData["ErrorMessage"] = "Database error. Please try again.";
                    return View();
                }

                // Create session
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserType", user.UserType);

                TempData["SuccessMessage"] = "Email verified successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification error for email: {Email}", email);
                TempData["ErrorMessage"] = $"Verification failed: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("VerifyEmail", new { email });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResendOTP(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Register");
                }

                if (user.IsEmailVerified)
                {
                    TempData["SuccessMessage"] = "Email already verified. Please log in.";
                    return RedirectToAction("Login");
                }

                // Generate new OTP
                var newOtp = GenerateOTP();
                user.OTP = newOtp;
                user.OTPExpiry = DateTime.UtcNow.AddMinutes(10);
                await _context.SaveChangesAsync();

                await SendVerificationEmail(user.Email, newOtp);

                TempData["SuccessMessage"] = "A new OTP has been sent to your email.";
                return RedirectToAction("VerifyEmail", new { email = user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP for email {Email}", email);
                TempData["ErrorMessage"] = "An error occurred while resending OTP.";
                return RedirectToAction("VerifyEmail", new { email });
            }
        }

        #endregion

        #region Helper Methods

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return null;
        }

        private IActionResult RedirectToDashboard(string userType = null)
        {
            userType ??= HttpContext.Session.GetString("UserType");

            // Add logging
            var userId = HttpContext.Session.GetInt32("UserID");
            _logger.LogInformation("RedirectToDashboard - UserId: {UserId}, UserType: {UserType}", userId, userType);

            return userType?.ToLower() switch
            {
                "admin" => RedirectToAction("AdminDashboard", "Admin"),
                "farmer" => RedirectToAction("FarmerDashboard", "Farmers"),
                "buyer" => RedirectToAction("BuyerDashboard", "Buyer"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        private void CreateUserSession(User user)
        {
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserType", user.UserType);
            HttpContext.Session.SetString("UserName", user.Name);
        }

        private async Task SendVerificationEmail(string email, string otp)
        {
            var verificationLink = Url.Action(
                "VerifyEmail",
                "Account",
                new { email, otp },
                protocol: Request.Scheme);

            var emailSubject = "Verify Your Email Address";
            var emailBody = $@"
                <h2>Welcome to Farmer Market Place!</h2>
                <p>Thank you for registering. Please verify your email address by:</p>
                <ol>
                    <li>Entering this OTP code: <strong>{otp}</strong></li>
                    <li>Or by clicking: <a href='{verificationLink}'>Verify Email</a></li>
                </ol>
                <p>This OTP expires in 10 minutes.</p>";

            await _emailService.SendEmailAsync(email, emailSubject, emailBody);
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedPasswordHash = Convert.FromBase64String(parts[1]);

            var enteredHash = KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            return CryptographicOperations.FixedTimeEquals(enteredHash, storedPasswordHash);
        }

        private string GenerateOTP()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] tokenData = new byte[4];
            rng.GetBytes(tokenData);
            return (BitConverter.ToUInt32(tokenData, 0) % 1000000).ToString("D6");
        }

        #endregion
    }
}