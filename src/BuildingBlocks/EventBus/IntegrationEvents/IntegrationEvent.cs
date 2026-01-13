using System.ComponentModel.DataAnnotations;

namespace IntegrationEvents
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        [Key]
        public Guid Id { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime CreationDate { get; }
    }
}
