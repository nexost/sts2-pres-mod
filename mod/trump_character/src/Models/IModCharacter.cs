namespace TrumpMod.Models;

/// <summary>
/// Marks characters added by this mod so patches can recognize them without hardcoding a class.
/// </summary>
public interface IModCharacter
{
	/// <summary>
	/// Character entry (lowercase) whose FMOD events we reuse for attack/cast/death sounds,
	/// because CharacterModel builds those paths from the Id and they aren't overridable.
	/// </summary>
	string SfxProxyEntry { get; }
}
