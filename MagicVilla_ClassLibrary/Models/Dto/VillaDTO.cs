using System.ComponentModel.DataAnnotations;

namespace MagicVilla_ClassLibrary.Models.Dto
{
    public class VillaDTO
    {
        public int Id { get; set; }

        [Required]
        [MinLength(3), MaxLength(30)]
        public string Name { get; set; }
        public int Occupancy { get; set; }
        public int Sqft { get; set; }
    }
}
