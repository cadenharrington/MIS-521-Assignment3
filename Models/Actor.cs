using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FL2024_Assignment3_charrington.Models
{
    public class Actor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Gender { get; set; }

        public int? Age { get; set; }

        [Display(Name = "IMDB Link")]
        public string? IMDBHyperlink { get; set; }

        [Display(Name = "Photo")]
        [DataType(DataType.Upload)]
        public byte[]? Photo { get; set; }

        public ICollection<MovieActor>? MovieActors { get; set; }
    }
}
