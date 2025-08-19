using Business;
using Business.Interface;
using DocumentFormat.OpenXml.Wordprocessing;
using Entities;
using LoginProject.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Data;


namespace LoginProject.Controllers
{
    public class MailController : Controller
    {
        private readonly IMailService _mailService;
        private readonly IUserService _userService; 

        public MailController(IMailService mailService, IUserService userService)
        {
            _mailService = mailService;
            _userService = userService;
        }

        [HttpPost]
        public IActionResult SendMail(int id) 
        {
            var user = _mailService.GetUserById(id);
            if (user == null) 
            {
                return NotFound(); 
            }

            Task.Run(() => _mailService.SendMailAsync(user));

            ViewBag.Message = "Mail has been sent!";
            var users = _userService.PreapareUserDto();
            return View("~/Views/Login/Success.cshtml",new UserList {IsAuthority = true, IsLogLogin = true, Users = users});

        }

    }
}
