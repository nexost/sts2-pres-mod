namespace PresMod.Characters.Biden.Relics;

/// <summary>
/// Ancient version of the Aviator Shades, given by Touch of Orobas: at the end of your turn, Doze 3; when you nod off,
/// gain 14 Block. Starter rarity like Black Blood, so it's never offered as a random reward.
/// </summary>
public sealed class DarkAviators : AviatorsRelic
{
	protected override decimal NapBlock => 14m;
}
