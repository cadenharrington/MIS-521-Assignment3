using System.ComponentModel.DataAnnotations.Schema;

namespace FL2024_Assignment3_charrington.Models
{
    public class MovieActor
    {
        public int Id { get; set; }

        [ForeignKey("Movie")]
        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }

        [ForeignKey("Actor")]
        public int? ActorId { get; set; }
        public Actor? Actor { get; set; }
    }
}
