using System.ComponentModel.DataAnnotations;

namespace VisitorLog_PBFD.Models
{
    public class ContinentRoot
    {
        [Key]
        public int PersonId { get; set; }
        public int Continent {  get; set; }
        public Boolean IsDeleted { get; set; }
        public Person? Person { get; set; } // Navigation property
    }
}
