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
        public IActionResult Loginn()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Loginn(BaseUser entity)
        {

            if (_userService.ValidateUser(entity.email, entity.password))
            {
                return View("Success");
            }
            else
            {
                return View("Error");
            }

        }

        public IActionResult Profile() 
        { 
            return View();
        }
    }
}
