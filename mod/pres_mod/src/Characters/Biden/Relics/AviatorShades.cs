using PresMod.Framework.Patches;

namespace PresMod.Characters.Biden.Relics;

/// <summary>Starter: at the end of your turn, Doze 2. When you nod off, gain 8 Block.</summary>
public sealed class AviatorShades : AviatorsRelic, IUpgradableStarterRelic
{
	protected override decimal NapBlock => 8m;

	/// <summary>What Touch of Orobas turns it into.</summary>
	public RelicModel AncientUpgrade => ModelDb.Relic<DarkAviators>();
}
