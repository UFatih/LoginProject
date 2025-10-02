using Business;
using Business.Interface;
using Entities;
using LoginProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using DocumentFormat.OpenXml.Spreadsheet;
using BCrypt.Net;
using System.Net;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Business.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace LoginProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMailService _mailService;
        private readonly ILocalizationService _localizationService;

        public LoginController(IUserService userService, IMailService mailService, ILocalizationService localizationService)
        {
            _userService = userService;
            _mailService = mailService;
            _localizationService = localizationService;
        }



        [HttpGet]
        public IActionResult Success()
        {
            var users = _userService.PreapareUserDto();
            var model = new UserList();
            model.Users = users;

            var userId = HttpContext.Session.GetInt32("CurrentUser");
            if (userId == null)
            {
                return RedirectToAction("Loginn", "Login");
            }

            if (userId != null)
            {
                var currentUser = _userService.GetUserById(userId.Value);
                ViewBag.Username = currentUser.username;
            }

            int? roleId = _userService.GetBaseUserRoleIdByUserId(userId.Value);
            var roles = _userService.GetUserRoles();
            model.IsAuthority = roles.Where(x => x.Id == roleId).Any(x => x.ischecked);
            model.IsLogLogin = roles.Where(x => x.Id == roleId).Any(x => x.islogloginchecked);

            return View(model);


        }


        [HttpPost] //Delete
        public IActionResult Success([FromBody] int Id)
        {
            _userService.UserDelete(Id);
            return Ok("User has been deleted.");
        }


        [HttpGet]
        public IActionResult ResetPassword(int id, string token)
        {
            var userPassReset = _mailService.GetUserByIdd(id);
            if (userPassReset == null || userPassReset.PasswordResetToken != token || userPassReset.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired link");

            }
            return View(new ResetPasswordViewModel { Id = id, Token = token });

        }

        [HttpPost]
        public IActionResult ResetPassword([FromBody] ResetPasswordViewModel modelPass)
        {
            if (string.IsNullOrEmpty(modelPass.NewPassword) || string.IsNullOrEmpty(modelPass.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords cannot be empty!");
                return View(modelPass);
            }

            var userPass = _userService.GetUserById(modelPass.Id);

            if (string.IsNullOrEmpty(userPass.PasswordResetToken) || userPass.PasswordResetToken != modelPass.Token)
            {
                return BadRequest("Invalid token");
            }

            if (BCrypt.Net.BCrypt.Verify(modelPass.NewPassword, userPass.password))
            {
                return BadRequest("New password cannot be the same as the old password!");
            }

            userPass.password = BCrypt.Net.BCrypt.HashPassword(modelPass.NewPassword);
            userPass.PasswordResetToken = null;
            userPass.PasswordResetTokenExpiresAt = null;

            _userService.UserUpdate(userPass);

            TempData["Success"] = "Password has been successfully updated!";
            return RedirectToAction("Loginn", "Login");
        }



        [HttpPost]
        public IActionResult ChangeLanguage(string language)
        {
            _localizationService.Load(language);
            HttpContext.Session.SetString("Language", language);

            var returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Loginn", "Login");
        }

        [HttpGet]
        public IActionResult ForgotPasswordPage() 
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Language")))
            {
                _localizationService.Load("tr");
                HttpContext.Session.SetString("Language", "tr");
            }
            else
            {
                var language = HttpContext.Session.GetString("Language");
                _localizationService.Load(language); 
            }

            return View(); 
        }


        [HttpGet] //Pdf for Users
        public IActionResult UsersPdf()
        {
            var users = _userService.GetAllUsers();

            using var ms = new MemoryStream();
            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Başlık
            var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 20, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.Blue);
            var title = new iTextSharp.text.Paragraph("User List", titleFont);
            title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            doc.Add(title);

            // Tarih
            var dateFont = iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.ITALIC, iTextSharp.text.BaseColor.Gray);
            doc.Add(new iTextSharp.text.Paragraph("Generated on: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), dateFont));
            doc.Add(new iTextSharp.text.Paragraph("\n")); // boş satır

            // Tablo
            var table = new iTextSharp.text.pdf.PdfPTable(4);
            table.WidthPercentage = 100;
            table.SpacingBefore = 20f;
            table.SpacingAfter = 20f;

            // Başlık satırı (stil)
            var headerFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.White);
            var headerBg = new iTextSharp.text.BaseColor(52, 152, 219); // mavi arkaplan
            string[] headers = { "ID", "Email", "Username", "Job" };

            foreach (var h in headers)
            {
                var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(h, headerFont));
                cell.BackgroundColor = headerBg;
                cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                cell.Padding = 5;
                table.AddCell(cell);
            }

            // Satırlar (stil)
            var rowFont = iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.Black);
            foreach (var u in users)
            {
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(u.Id.ToString(), rowFont)) { Padding = 5 });
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(u.email ?? "", rowFont)) { Padding = 5 });
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(u.username ?? "", rowFont)) { Padding = 5 });
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(u.job ?? "", rowFont)) { Padding = 5 });
            }

            doc.Add(table);

            // Footer
            var footerFont = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.ITALIC, iTextSharp.text.BaseColor.Gray);
            var footer = new iTextSharp.text.Paragraph("© 2025 - LoginProject | Confidential", footerFont);
            footer.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            doc.Add(footer);

            doc.Close();

            var bytes = ms.ToArray();
            return File(bytes, "application/pdf", "Users.pdf");
        }




        [HttpGet] //Excel for Users
        public IActionResult UsersExcel()
        {
            var users = _userService.GetAllUsers();

            var dtEmails = new DataTable("UsersEmailByUsername");

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in users)
            {
                var colName = string.IsNullOrWhiteSpace(u.username) ? "Unnamed" : u.username.Trim();
                if (!used.Add(colName))
                {
                    int i = 2;
                    while (!used.Add($"{colName}_{i}")) i++;
                    colName = $"{colName}_{i}";
                }
                dtEmails.Columns.Add(colName);
            }

            //Adding Email
            var emailsRow = dtEmails.NewRow();
            int colIndex = 0;
            foreach (var u in users)
            {
                emailsRow[colIndex++] = u.email;
            }
            dtEmails.Rows.Add(emailsRow);

            // Password & Job, 2. Page
            var dtPassJob = new DataTable("PasswordAndJob");
            dtPassJob.Columns.Add("Username");
            dtPassJob.Columns.Add("Password");
            dtPassJob.Columns.Add("Job");

            foreach (var u in users)
            {
                dtPassJob.Rows.Add(
                    string.IsNullOrWhiteSpace(u.username) ? "Unnamed" : u.username.Trim(),
                    u.password,
                    u.job
                );
            }


            using var wb = new XLWorkbook();

            // Sayfa 1 - Email
            var ws1 = wb.Worksheets.Add("Users");
            ws1.Cell(1, 1).InsertTable(dtEmails, true);
            ws1.Columns().AdjustToContents();
            ws1.Rows().AdjustToContents();

            // Sayfa 2 - Password + Job
            var ws2 = wb.Worksheets.Add("Users2");
            ws2.Cell(1, 1).InsertTable(dtPassJob, true);
            ws2.Columns().AdjustToContents();
            ws2.Rows().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Users.xlsx"
            );
        }


        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Loginn()
        {
            //string tr = "tr";
            //ChangeLanguage(tr);

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Language")))
            {
                _localizationService.Load("tr");
                HttpContext.Session.SetString("Language", "tr");
            }
            else
            {
                var lang = HttpContext.Session.GetString("Language");
                _localizationService.Load(lang);
            }
            return View();
        }

        [HttpPost]
        public IActionResult Loginn([FromBody] UserProfile entity)
        {
            HttpContext.Session.SetString("IsLoggedIn", "true");

            const int Max_Fails = 3;
            const int Locked_Time = 1; // 1 min
            

            var user = _userService.GetAllUsers()
                .FirstOrDefault(u => u.email == entity.User.email);

            // Lock Control
            if (user.LockedUntilUTC.HasValue && user.LockedUntilUTC.Value > DateTime.UtcNow)
            {
                // Fail log
                var lockRemaining = (int)(user.LockedUntilUTC.Value - DateTime.UtcNow).TotalSeconds;

                var role = _userService.GetUserRolesByUserId(user.Id);
                var roleName = role.FirstOrDefault()?.name ?? "User"; 

                var failedlogLocked = new UserLoginLog
                {
                    UserId = user.Id,
                    UserName = user.username,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    LoginDate = DateTime.Now, // loglar local time tutuyor istersen UTC yap
                    BrowserInfo = Request.Headers["User-Agent"].ToString(),
                    Role = roleName,
                    IsSuccess = false
                };
                _userService.AddUserLoginLog(failedlogLocked);

                return Json(new
                {
                    success = false,
                    locked = true,
                    remainingSeconds = lockRemaining,
                    message = $"Hesabınız kilitlendi! {lockRemaining} saniye sonra tekrar deneyebilirsiniz."
                });
            }

            if (user != null)
            {
                bool isPasswordValid = false;

                // Password
                if (!user.password.StartsWith("$2a$") && !user.password.StartsWith("$2b$"))
                {

                    if (user.password == entity.User.password)
                    {
                        isPasswordValid = true;
                        user.password = BCrypt.Net.BCrypt.HashPassword(entity.User.password);
                        _userService.UserUpdate(user);
                    }
                }
                else
                {
                    if (BCrypt.Net.BCrypt.Verify(entity.User.password, user.password))
                    {
                        isPasswordValid = true;
                    }
                }

                // login logs
                if (isPasswordValid) //The flag(isPasswordValid) is used to determine whether the password is correct and to log the user in if so.
                {
                    user.FailedLoginCount = 0;
                    user.LockedUntilUTC = null;
                    _userService.UserUpdate(user);

                    HttpContext.Session.SetInt32("CurrentUser", user.Id);
                    var roles = _userService.GetUserRolesByUserId(user.Id);
                    var isAdmin = roles.Any(r => r.ischecked);
                    HttpContext.Session.SetString("IsAuthority", isAdmin ? "true" : "false");

                    var firstRole = roles.FirstOrDefault();
                    if (firstRole != null)
                    {
                        HttpContext.Session.SetInt32("RoleId", firstRole.Id);
                    }

                    // Success Login ✅
                    string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (ipAddress == "::1") ipAddress = "192.168.1.16";

                    var role = _userService.GetUserRolesByUserId(user.Id);
                    var roleName = role.FirstOrDefault()?.name ?? "User";

                    var loglogin = new UserLoginLog
                    {
                        UserId = user.Id,
                        UserName = user.username,
                        IPAddress = ipAddress,
                        LoginDate = DateTime.Now,
                        BrowserInfo = Request.Headers["User-Agent"].ToString(),
                        Role = roleName,
                        IsSuccess = true                         
                    };
                    _userService.AddUserLoginLog(loglogin);

                    return Json(new { success = true });
                }

                if (!isPasswordValid) // Wrong Password
                {
                    user.FailedLoginCount += 1;
                    if (user.FailedLoginCount >= Max_Fails) 
                    {
                        user.LockedUntilUTC = DateTime.UtcNow.AddMinutes(Locked_Time); // Locking
                        user.FailedLoginCount = 0;
                    }
                    _userService.UserUpdate(user);

                    // Failed Login ❌
                    string failedloginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (failedloginIp == "::1") failedloginIp = "192.168.1.16";

                    var role = _userService.GetUserRolesByUserId(user.Id); 
                    var roleName = role.FirstOrDefault()?.name ?? "User";

                    var failedlog = new UserLoginLog
                    {
                        UserId = user?.Id ?? 0,
                        UserName = user?.username,
                        IPAddress = failedloginIp,
                        LoginDate = DateTime.Now,
                        BrowserInfo = Request.Headers["User-Agent"].ToString(),
                        Role = roleName,
                        IsSuccess = false
                    };
                    _userService.AddUserLoginLog(failedlog);
                }         

            }

            return Json(new
            {
                success = false,
                locked = false,
                message = LocalizationCache.Get("Email or password is incorrect!")
            });

        }

        [HttpGet] //Add, Edit
        public IActionResult Profile(int? Id)
        {
            if (Id == null)
            {
                var emptyProfile = new UserProfile
                {
                    User = new BaseUser(),
                    roles = new List<UserRole>(),
                    RolesSelectListItem = _userService.GetUserRoles()
                        .Select(r => new System.Web.Mvc.SelectListItem
                        {
                            Text = r.name,
                            Value = r.Id.ToString(),
                        }).ToList()
                };

                return View(emptyProfile);
            }
            else
            {
                var userProfile = _userService.GetUserProfileById(Id.Value);
                return View(userProfile);
            }
        }


        [HttpPost]
        public IActionResult Profile([FromBody] UserProfile model)
        {
            if (model == null || model.User == null)
            {
                return BadRequest(new { success = false, message = LocalizationCache.Get("Missing user data") });
            }

            if (string.IsNullOrEmpty(model.User.password))
            {
                return BadRequest(new { success = false, message = LocalizationCache.Get("Password cannot be empty") });
            }

            if (!string.IsNullOrEmpty(model.RepeatPassword) && model.User.password != model.RepeatPassword)
            {
                return BadRequest(new { success = false, message = LocalizationCache.Get("Passwords do not match") });
            }


            BaseUser user;
            bool isNewUser = false;

            if (model.User.Id == 0 || _userService.GetUserById(model.User.Id) == null)
            {
                // New user, Adding
                user = new BaseUser
                {
                    email = model.User.email,
                    username = model.User.username,
                    password = BCrypt.Net.BCrypt.HashPassword(model.User.password),
                    job = model.User.job
                };
                _userService.UserAdd(user);
                isNewUser = true;
            }
            else
            {
                // Current user, Updating 
                user = _userService.GetUserById(model.User.Id);

                if (BCrypt.Net.BCrypt.Verify(model.User.password, user.password))
                {
                    return BadRequest(new { success = false, message = LocalizationCache.Get("New password cannot be the same as the old password!") });
                }

                user.email = model.User.email;
                user.username = model.User.username;
                user.password = BCrypt.Net.BCrypt.HashPassword(model.User.password);
                user.job = model.User.job;
                _userService.UserUpdate(user);
                _userService.DeleteRolesByUserId(user.Id);
            }


            // Roller
            if (model.SelectedRoleIds != null && model.SelectedRoleIds.Any())
            {
                foreach (var roleId in model.SelectedRoleIds)
                {
                    _userService.AddUserRole(user.Id, roleId);
                }
            }


            return Ok(new { success = true, message = isNewUser ? LocalizationCache.Get("User has been successfully added") : LocalizationCache.Get("User has been successfully updated") });
        }


        [HttpGet]
        public IActionResult Roles()
        {
            var userRoles = _userService.GetUserRoles();
            return View(userRoles);
        }


        [HttpPost] //Delete
        public IActionResult Roles([FromBody] int Id)
        {
            if (_userService.IsRoleAssigned(Id))
            {
                return BadRequest(LocalizationCache.Get("This role is assigned to one or more users and cannot be deleted."));
            }
            _userService.RoleDelete(Id);
            return Ok(LocalizationCache.Get("Role has been deleted."));
        }


        [HttpGet] //Role --> Add, Edit
        public IActionResult AddRoles(int? Id)
        {
            if (Id == null)
            {
                return View(new UserRole());
            }
            else
            {
                var roleprofile = _userService.GetRoleById(Id.Value);
                return View(roleprofile);
            }

        }

        [HttpPost] //Role --> Update + New User operation
        public IActionResult AddRoles([FromBody] UserRole entity2)
        {
            if (entity2.Id == null)
            {
                _userService.AddingRoles(entity2);
                return Ok(new { Message = LocalizationCache.Get("User has been added") });
            }
            else
            {
                _userService.UpdateRole(entity2);
                return Ok(new { Message = LocalizationCache.Get("User has been updated") });
            }

        }

        [HttpGet]
        public IActionResult LoginLogs()
        {
            var filtered = new LoginLogFilterViewModel
            {
                StartDate = DateTime.Now.AddDays(-14),
                EndDate = DateTime.Now
            };

            filtered.Results = _userService.GetLoginLogs(null, null, filtered.StartDate, filtered.EndDate, null, null, null);

            return View(filtered);
        }

        [HttpPost]
        public IActionResult LoginLogs(LoginLogFilterViewModel filtered)
        {
            if (!filtered.StartDate.HasValue)
                filtered.StartDate = DateTime.Now.AddDays(-14);

            if (!filtered.EndDate.HasValue)
                filtered.EndDate = DateTime.Now;

            filtered.EndDate = filtered.EndDate.Value.Date.AddDays(1).AddTicks(-1);

            bool? isSuccessFilter = filtered.IsSuccess;

            filtered.Results = _userService.GetLoginLogs(
                filtered.UserName,
                filtered.IPAddress,
                filtered.StartDate,
                filtered.EndDate,
                filtered.BrowserInfo,
                filtered.Role,
                isSuccessFilter
            );

            return View(filtered);
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _userService.Logout();
            return RedirectToAction("Loginn", "Login");
        }


    }
}
