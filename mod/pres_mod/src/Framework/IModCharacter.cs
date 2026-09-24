namespace PresMod.Framework;

/// <summary>
/// Marks characters added by this mod so the Framework patches can recognize them without hardcoding a class.
/// Every character class (Characters/&lt;Name&gt;/&lt;Name&gt;.cs) implements it; the defaults suit a human-sized figure.
/// </summary>
public interface IModCharacter
{
	/// <summary>
	/// Character entry (lowercase) whose FMOD events we reuse for attack/cast/death sounds,
	/// because CharacterModel builds those paths from the Id and they aren't overridable.
	/// </summary>
	string SfxProxyEntry { get; }

	/// <summary>
	/// Height in pixels of the combat body, feet to top of head (Ironclad is about 280). Used when the combat scene is
	/// a sprite of generated paintings (NCharacterPoses) rather than a Spine rig.
	/// </summary>
	float CombatFigureHeight => 290f;

	/// <summary>Height of the shop painting (images/&lt;id&gt;/merchant_pose.png) in the shop scene's own units.</summary>
	float MerchantFigureHeight => 330f;

	/// <summary>Height of the rest-site painting (images/&lt;id&gt;/rest_site_pose.png); the rest-site scene is scaled 0.76.</summary>
	float RestSiteFigureHeight => 560f;
}
