using Entities;

namespace LoginProject.Models
{
    public class LoginLogFilterViewModel 
    {
        public string? UserName { get; set; }
        public string? IPAddress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? BrowserInfo { get; set; }
        public string? Role { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool? IsSuccess { get; set; }

        public List<UserLoginLog>? Results { get; set; } 
 
    }
}
