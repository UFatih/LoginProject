using Business;
using Business.Interface;
using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Data;


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

            return View(model);


        }


        //[HttpGet] 
        //public IActionResult Success()
        //{
        //    var users = _userService.PreapareUserDto();
        //    var model = new UserList();
        //    model.Users = users;

        //    var userId= HttpContext.Session.SetInt32("CurrentUser", user.Id);
        //    int? roleId = _userService.GetBaseUserRoleIdByUserId(userId);
        //    var roles = _userService.GetUserRoles();
        //    roles.Where(x => x.Id == roleId).ToList();
        //    model.IsAuthority= roles.Any(x => x.ischecked);


        //    return View(model);

        //}

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
            var user = _userService.GetAllUsers()
                .FirstOrDefault(u => u.email == entity.User.email && u.password == entity.User.password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("CurrentUser", user.Id);

                var roles = _userService.GetUserRolesByUserId(user.Id);
                var isAdmin = roles.Any(r => r.ischecked); 

                HttpContext.Session.SetString("IsAuthority", isAdmin ? "True" : "False");



                return Json(true);
            }

            return Json(false);
        }

        //[HttpPost]
        //public IActionResult Loginn([FromBody] BaseUser entity)
        //{
        //    var user = _userService.GetAllUsers()
        //        .FirstOrDefault(u => u.email == entity.email && u.password == entity.password);

        //    if (user != null)
        //    {
        //        HttpContext.Session.SetInt32("CurrentUser", user.Id);

        //        var roles = _userService.GetUserRolesByUserId(user.Id);
        //        var roleNames = roles.Select(r => r.name).ToList();
        //        HttpContext.Session.SetString("UserRole", string.Join(",", roleNames));


        //        return Json(true);
        //    }

        //    return Json(false);
        //}



        //[HttpPost] //Login Page
        //public IActionResult Loginn([FromBody] BaseUser entity)
        //{
        //    var user = _userService.GetAllUsers()
        //        .FirstOrDefault(u => u.email == entity.email && u.password == entity.password);

        //    if (user != null)
        //    {
        //        HttpContext.Session.SetInt32("CurrentUser", user.Id);

        //        int? roleId = _userService.GetBaseUserRoleIdByUserId(user.Id);

        //        if (roleId != null)
        //        {
        //            HttpContext.Session.SetInt32("CurrentUserRole", roleId.Value);
        //        }

        //        var roles = _userService.GetUserRoles(); 
        //        var userHasAuthority = roles.Any(r => r.Id == roleId && r.ischecked); 
        //        HttpContext.Session.SetString("IsAuthority", userHasAuthority.ToString());

        //        return Json(true);
        //    }

        //    return Json(false);
        //}


        //[HttpPost] //Login Page
        //public IActionResult Loginn([FromBody] BaseUser entity)
        //{
        //    var user = _userService.GetAllUsers()
        //        .FirstOrDefault(u => u.email == entity.email && u.password == entity.password);

        //    if (user != null)
        //    {

        //        HttpContext.Session.SetInt32("CurrentUser", user.Id);

        //        int? roleId = _userService.GetBaseUserRoleIdByUserId(user.Id);

        //        if (roleId != null)
        //        {
        //            HttpContext.Session.SetInt32("CurrentUserRole", roleId.Value);
        //        }

        //        return Json(true);
        //    }

        //    return Json(false);
        //}




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

            return Ok(new { success = true, message = "Kullanıcı başarıyla güncellendi." });
        }



        //[HttpPost]
        //public IActionResult Profile([FromBody] UserProfile model)
        //{

        //    var user = _userService.GetUserById(model.User.Id);
        //    if (user == null) return NotFound();

        //    user.email = model.User.email;
        //    user.password = model.User.password;
        //    user.username = model.User.username;
        //    user.job = model.User.job;
        //    _userService.UserUpdate(user);

        //    _userService.DeleteRolesByUserId(user.Id);

        //    if (model.SelectedRoleIds != null)
        //    {
        //        foreach (var roleId in model.SelectedRoleIds)
        //        {
        //            _userService.AddUserRole(user.Id, roleId);
        //        }
        //    }

        //    return Ok(new { message = "User updated." });
        //}





        //[HttpPost] // Update + New User operation
        //public IActionResult Profile([FromBody] BaseUser entity)
        //{
        //    if (entity.Id == null)
        //    {
        //        _userService.UserAdd(entity);
        //        return Ok(new { Message = "User has been added" });
        //    }
        //    else
        //    {
        //        _userService.UserUpdate(entity);
        //        return Ok(new { Message = "User has been updated" });
        //    }
        //}



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


        //[HttpPost] //Delete
        //public IActionResult Roles([FromBody] int Id)
        //{
        //    _userService.RoleDelete(Id);
        //    return Ok("User has been deleted.");
        //}

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

    }
}
