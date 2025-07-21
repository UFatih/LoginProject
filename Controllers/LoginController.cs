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
            return View(); 
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

        public IActionResult Profile() 
        { 
            return View();
        }
    }
}
