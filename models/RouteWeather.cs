using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class RouteWeather
    {
        [Key]
        public int RouteWeatherId { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        [MaxLength(200)]
        public string? Forecast { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        public decimal? Temperature { get; set; }

        [MaxLength(1000)]
        public string? RouteDescription { get; set; }

        [MaxLength(500)]
        public string? RouteMapUrl { get; set; }
    }
}