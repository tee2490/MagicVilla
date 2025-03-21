using Microsoft.AspNetCore.Identity;

namespace MagicVilla_ClassLibrary.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
