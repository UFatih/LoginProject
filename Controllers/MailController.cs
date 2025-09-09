using Business;
using Business.Interface;
using DocumentFormat.OpenXml.Spreadsheet;
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
            var user = _mailService.GetUserByIdd(id);
            if (user == null)
            {
                return NotFound();
            }

            Task.Run(() => _mailService.SendMailAsync(user));

            ViewBag.Message = "Mail has been sent!";
            var users = _userService.PreapareUserDto();
            return View("~/Views/Login/Success.cshtml", new UserList { IsAuthority = true, IsLogLogin = true, Users = users });

        }



        [HttpPost]
        public async Task<IActionResult> ForgotPasswordLink(int id)
        {
            var userPass = _mailService.GetUserByIdd(id);
            if (userPass == null)
                return NotFound();

            var token = Guid.NewGuid().ToString();
            userPass.PasswordResetToken = token;
            userPass.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            _userService.UserUpdate(UserMapper.ToEntity(userPass));

            var resetLink = Url.Action(
                "ResetPassword", "Login",
                new { id = userPass.Id, token = token },     
                Request.Scheme);

            // Send mail html format
            await _mailService.SendPasswordResetLinkAsync(
                receiverMail: "fatihkusaslan12345@gmail.com",
                resetLink: resetLink,
                userEmail: userPass.email


            );

            TempData["Success"] = "Password reset link has been sent to your email!";
            return RedirectToAction("Success", "Login");
        }


    }
}
