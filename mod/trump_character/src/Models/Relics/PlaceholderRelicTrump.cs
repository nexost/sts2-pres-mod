using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TrumpMod.Models.Relics;

/// <summary>Step 2 stand-in starter relic (heals after combat, like Burning Blood). Replaced in Step 4.</summary>
public sealed class PlaceholderRelicTrump : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new HealVar(6m) };

	public override async Task AfterCombatVictory(CombatRoom _)
	{
		if (!Owner.Creature.IsDead)
		{
			Flash();
			await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
		}
	}
}
