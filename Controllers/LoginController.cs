using Business;
using Business.Interface;
using Entities;
using LoginProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using ClosedXML.Excel;



namespace LoginProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUserService _userService;

        public LoginController(IUserService userService)
        {
            _userService = userService;
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
                return RedirectToAction("Login", "Home");
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
            return View();
        }


        [HttpPost]
        public IActionResult Loginn([FromBody] UserProfile entity)
        {

            HttpContext.Session.SetString("IsLoggedIn", "true");

            var user = _userService.GetAllUsers()
                .FirstOrDefault(u => u.email == entity.User.email && u.password == entity.User.password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("CurrentUser", user.Id);

                var roles = _userService.GetUserRolesByUserId(user.Id);
                var isAdmin = roles.Any(r => r.ischecked);

                HttpContext.Session.SetString("IsAuthority", isAdmin ? "true" : "false");

                var firstRole = roles.FirstOrDefault();
                if (firstRole != null)
                {
                    HttpContext.Session.SetInt32("RoleId", firstRole.Id);
                }

                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (ipAddress == "::1") //localhost
                {
                    ipAddress = "192.168.1.16";
                }
                var loglogin = new UserLoginLog
                {
                    UserId = user.Id,
                    UserName = user.username,
                    IPAddress = ipAddress,
                    LoginDate = DateTime.Now,
                    BrowserInfo = Request.Headers["User-Agent"].ToString()
                };
                _userService.AddUserLoginLog(loglogin);



                return Json(true);
            }

            return Json(false);
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
                return BadRequest(new { success = false, message = "Missing user data" });
            }


            var user = _userService.GetUserById(model.User.Id);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User cannot found." });
            }

            user.email = model.User.email;
            user.password = model.User.password;
            user.username = model.User.username;
            user.job = model.User.job;

            _userService.UserUpdate(user);

            _userService.DeleteRolesByUserId(user.Id);

            if (model.SelectedRoleIds != null && model.SelectedRoleIds.Any())
            {
                foreach (var roleId in model.SelectedRoleIds)
                {
                    _userService.AddUserRole(user.Id, roleId);
                }
            }

            return Ok(new { success = true, message = "User has been successfully updated" });
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
                return BadRequest("This role is assigned to one or more users and cannot be deleted.");
            }
            _userService.RoleDelete(Id);
            return Ok("Role has been deleted.");
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
                return Ok(new { Message = "User has been added" });
            }
            else
            {
                _userService.UpdateRole(entity2);
                return Ok(new { Message = "User has been updated" });
            }

        }

        [HttpGet]
        public IActionResult LoginLogs()
        {
            //var roleId = HttpContext.Session.GetInt32("RoleId");
            //if (roleId != 1)
            //{
            //    return Unauthorized();
            //}

            var filtered = new LoginLogFilterViewModel
            {
                StartDate = DateTime.Now.AddDays(-14),
                EndDate = DateTime.Now
            };

            filtered.Results = _userService.GetLoginLogs(
                null, null, filtered.StartDate, filtered.EndDate, null);

            return View(filtered);
        }

        [HttpPost]
        public IActionResult LoginLogs(LoginLogFilterViewModel filtered)
        {
            //var roleId = HttpContext.Session.GetInt32("RoleId");
            //if (roleId != 1)
            //{
            //    return Unauthorized();
            //}
            if (!filtered.StartDate.HasValue)
                filtered.StartDate = DateTime.Now.AddDays(-14);

            if (!filtered.EndDate.HasValue)
                filtered.EndDate = DateTime.Now;

            filtered.Results = _userService.GetLoginLogs(
                filtered.UserName,
                filtered.IPAddress,
                filtered.StartDate,
                filtered.EndDate,
                filtered.BrowserInfo
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
