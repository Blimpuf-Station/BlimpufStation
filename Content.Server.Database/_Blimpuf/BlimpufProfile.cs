using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Content.Server.Database._Blimpuf;

[Table("blimpuf_profile")]
public sealed class BlimpufProfile
{
    [Key, ForeignKey(nameof(Profile))]
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = null!;

    public string? SecondaryGender { get; set; }
}
