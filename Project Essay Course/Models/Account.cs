using Microsoft.AspNetCore.Identity;

namespace Project_Essay_Course.Models
{
    public class Account : IdentityUser
    {
        public DateTime? CreatedAt { get; set; }

    }
}
