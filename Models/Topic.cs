using System.ComponentModel.DataAnnotations;

namespace MESSGEBROKER.Models
{
    public class Topic
    {
        [Key]
        public int Id { set; get; }

        [Required]
        public string? Name { set; get; }
    }
}
