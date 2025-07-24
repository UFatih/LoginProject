using Business;
using Business.Interface;
using Entities;
using Microsoft.AspNetCore.Mvc;

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
            var users = _userService.GetAllUsers();  
            return View(users);
            
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


        

        [HttpPost] //Login Page
        public IActionResult Loginn([FromBody] BaseUser entity)
        {

            if (_userService.ValidateUser(entity.email, entity.password))
            {
                //return View("Success");
                return Json(true);
            }
            else
            {
                //return View("Error");
                return Json(false);
            }

        }


        //[HttpGet] // Edit
        //public IActionResult Profile(int Id)
        //{
        //    var userprofile = _userService.GetUserById(Id);
        //    return View(userprofile);

        //}


        [HttpGet] //Add, Edit
        public IActionResult Profile(int? Id)
        {
            if (Id == null)
            {
                return View(new BaseUser());
            }
            else
            {
                var userprofile = _userService.GetUserById(Id.Value);
                return View(userprofile);
            }

        }


        [HttpPost] // Update + New User operation
        public IActionResult Profile([FromBody] BaseUser entity)
        {
            if (entity.Id == null)
            {
                _userService.UserAdd(entity);
                return Ok(new { Message = "User has been added" });
            }
            else
            {
                _userService.UserUpdate(entity);
                return Ok(new { Message = "User has been updated" });
            }
        }

        //[HttpPost] // Update, Add Method Update
        //public IActionResult Profile([FromBody] BaseUser entity) 
        //{
        //    _userService.UserUpdate(entity);
        //    return Ok(new { Message = "User has been updated" });
        //    //return RedirectToAction("Success");

        //}

    }
}
