using System.ComponentModel.DataAnnotations.Schema;

namespace Content.Server.Database;

public sealed class EmberfallModel
{
    public class EmberfallProfile
    {
        [Column("emberfallprofile_id")]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public string? CustomSpeciesName { get; set; }
    }
}
