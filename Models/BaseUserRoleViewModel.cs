using Microsoft.AspNetCore.Mvc;

namespace LoginProject.Models
{
    public class BaseUserRoleViewModel : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
