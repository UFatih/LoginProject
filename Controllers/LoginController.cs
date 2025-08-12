using Business;
using Business.Interface;
using Entities;
using LoginProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection.Metadata.Ecma335;


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


        //[HttpGet] //Filter Log
        //public IActionResult LoginLogs(LoginLogFilterViewModel filtered)
        //{

        //    var roleId = HttpContext.Session.GetInt32("RoleId");
        //    if (roleId != 1)
        //    {
        //        return Unauthorized();
        //    }

        //    if (!filtered.StartDate.HasValue)
        //    {
        //        filtered.StartDate = DateTime.Now.AddDays(-14);
        //    }


        //    if (!filtered.EndDate.HasValue)
        //    {
        //        filtered.EndDate = DateTime.Now;
        //    }

        //    var urLogs = _userService.GetLoginLogs(
        //        filtered.UserName,
        //        filtered.IPAddress,
        //        filtered.StartDate,
        //        filtered.EndDate,
        //        filtered.BrowserInfo
        //         );

        //    filtered.Results = urLogs;
        //    return View(filtered);

        //}


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _userService.Logout();
            return RedirectToAction("Loginn", "Login");
        }


    }
}
