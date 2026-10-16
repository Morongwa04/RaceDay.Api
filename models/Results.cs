using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [ForeignKey(nameof(EnrolmentId))]
        public Enrolment? Enrolment { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required]
        public int CapturedByOrganiserId { get; set; }

        [ForeignKey(nameof(CapturedByOrganiserId))]
        public User? CapturedByOrganiser { get; set; }
    }
}