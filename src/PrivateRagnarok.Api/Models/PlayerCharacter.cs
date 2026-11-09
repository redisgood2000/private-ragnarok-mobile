namespace PrivateRagnarok.Api.Models;

public sealed class PlayerCharacter
{
    public Guid PlayerId { get; set; }
    public required string CharacterName { get; set; }
    public string JobClass { get; set; } = "Novice";
    public int Level { get; set; } = 1;
    public long BaseExperience { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; } = -7f;
    public float Yaw { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
