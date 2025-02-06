using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using Mailo.Data.Enums;

namespace Mailo.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        public IActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Registration(RegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // التحقق من صحة البريد الإلكتروني
                try
                {
                    var mailAddress = new System.Net.Mail.MailAddress(model.Email);
                }
                catch (FormatException)
                {
                    ModelState.AddModelError("", "The email address format is invalid.");
                    return View(model);
                }

                // التحقق إذا كان البريد الإلكتروني موجودًا بالفعل
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "The email is already in use.");
                    return View(model);
                }

                // التحقق إذا كان رقم الهاتف موجودًا بالفعل
                if (_context.Users.Any(u => u.PhoneNumber == model.PhoneNumber))
                {
                    ModelState.AddModelError("", "The phone number is already in use.");
                    return View(model);
                }

                // إنشاء حساب جديد
                var account = new User
                {
                    Email = model.Email,
                    FName = model.FName,
                    LName = model.LName,
                    Password = model.Password,
                    Username = model.Username,
                    PhoneNumber = model.PhoneNumber,
                    Governorate = model.Governorate,
                    Address = model.Address,
                    Gender = model.Gender,
                    UserType = model.UserType,
                    EmailVerificationToken = GenerateVerificationCode(),
                    EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(30),
                    IsEmailVerified = false,
                };

                // إضافة الحساب إلى قاعدة البيانات
                _context.Users.Add(account);
                await _context.SaveChangesAsync();

                // إرسال البريد الإلكتروني للتحقق
                await SendVerificationEmail(account.Email, account.EmailVerificationToken);

                // عرض رسالة توضيحية للمستخدم
                ViewBag.Message = "A verification code has been sent to your email. Please check your inbox.";
                return View("VerifyCode");
            }

            // في حالة وجود خطأ في البيانات المدخلة
            return View(model);
        }


        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        [HttpGet]
        public IActionResult VerifyCode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmailVerificationToken == code && u.EmailVerificationTokenExpiry > DateTime.UtcNow);
            if (user != null)
            {
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                _context.SaveChanges();

                ViewBag.Message = "Your email has been verified successfully!";
                return View("Login");
            }
            ViewBag.Message = "Invalid or expired code.";
            return View();
        }

        private async Task SendVerificationEmail(string email, string code)
        {
            try
            {
                // Validate the email address
                var mailAddress = new MailAddress(email);

                var message = new MailMessage
                {
                    From = new MailAddress("mailostoreee@gmail.com"),
                    Subject = "Your Verification Code",
                    Body = $"Your verification code is: <strong>{code}</strong>. This code will expire in 30 minutes.",
                    IsBodyHtml = true
                };
                message.To.Add(email);

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("mailostoreee@gmail.com", "zrck gmqn cwzh bveq");
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(message);
                }
            }
            catch (FormatException ex)
            {
                ModelState.AddModelError("", "The email address format is invalid.");
            }
        }


        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(x => x.Username == model.UsernameOrEmail || x.Email == model.UsernameOrEmail);

                if (user != null && user.Password == model.Password)
                {
                    if ((bool)!user.IsEmailVerified)
                    {
                        // إعادة توليد رمز تحقق جديد
                        user.EmailVerificationToken = GenerateVerificationCode();
                        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(30);
                        await _context.SaveChangesAsync();

                        // إرسال الرمز الجديد
                        await SendVerificationEmail(user.Email, user.EmailVerificationToken);

                        // حفظ البريد في الجلسة لإعادة توجيهه بعد ذلك
                        TempData["UnverifiedEmail"] = user.Email;

                        // توجيه المستخدم إلى صفحة التحقق
                        return RedirectToAction("VerifyCode");
                    }

                    // إنشاء الجلسة بعد تسجيل الدخول بنجاح
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("Name", user.FName),
                new Claim(ClaimTypes.Role, user.UserType.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Username/Email or Password is not correct.");
                }
            }
            return View(model);
        }


        public IActionResult LogOut()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Edit()
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                var model = new EditUserViewModel
                {
                    FName = user.FName,
                    LName = user.LName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Governorate = user.Governorate,
                    Email = user.Email, // Displayed as read-only
                    Username = user.Username, // Displayed as read-only
                    Gender = user.Gender,
                    UserType = user.UserType
                };
                return View(model);
            }
            return NotFound();
        }


        [HttpPost]
        [Authorize]
        public IActionResult Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var email = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user != null)
                {
                    user.FName = model.FName;
                    user.LName = model.LName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Address = model.Address;
                    user.Governorate = model.Governorate;
                    user.Gender = model.Gender;

                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Your data has been successfully modified!";

                    return RedirectToAction("Edit");
                }
            }

            return View(model);
        }







        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    user.PasswordResetToken = Guid.NewGuid().ToString();
                    user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                    _context.SaveChanges();

                    await SendResetPasswordEmail(user.Email, user.PasswordResetToken);

                    ViewBag.Message = "A password reset link has been sent to your email.";
                    return View();
                }
                ModelState.AddModelError("", "Email not found.");
            }
            return View(model);
        }

        public IActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Message = "Invalid or expired token.";
                return View("Error");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.PasswordResetToken == model.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user != null)
                {
                    user.Password = model.NewPassword;
                    user.PasswordResetToken = null;
                    user.PasswordResetTokenExpiry = null;

                    _context.SaveChanges();

                    ViewBag.Message = "Your password has been reset successfully.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Invalid or expired token.");
            }
            return View(model);
        }


        private async Task SendResetPasswordEmail(string email, string token)
        {
            var resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

            var message = new MailMessage();
            message.From = new MailAddress("mailostoreee@gmail.com");
            message.To.Add(email);
            message.Subject = "Reset Password";
            message.Body = $"Please reset your password by clicking here: <a href=\"{resetLink}\">link</a>";
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("mailostoreee@gmail.com", "zrck gmqn cwzh bveq");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(message);
            }
        }
    }
}