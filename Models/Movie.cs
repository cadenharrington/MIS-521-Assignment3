using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FL2024_Assignment3_charrington.Models
{
    public class Movie
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Display(Name = "IMDB Link")]
        public string? IMDBHyperlink { get; set; }

        public string? Genre { get; set; }

        [Display(Name = "Release Year")]
        public int? YearOfRelease { get; set; }

        [Display(Name = "Poster")]
        [DataType(DataType.Upload)]
        public byte[]? PosterURL { get; set; }


        public ICollection<MovieActor>? MovieActors { get; set; }
    }
}
