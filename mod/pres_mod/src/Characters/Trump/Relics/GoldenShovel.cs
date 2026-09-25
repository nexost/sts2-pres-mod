using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Mechanics;
using PresMod.Framework.Patches;

namespace PresMod.Characters.Trump.Relics;

/// <summary>
/// Starter: at the start of your turn, Build 2. The construction crew never stops, so the Wall reaches its stages on its
/// own in longer fights. Same timing as Bound Phylactery: turn 1 before combat starts, later turns after the energy reset.
/// </summary>
public sealed class GoldenShovel : RelicModel, IUpgradableStarterRelic
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	/// <summary>What Touch of Orobas turns it into.</summary>
	public RelicModel AncientUpgrade => ModelDb.Relic<DiamondShovel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Build", 2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public override async Task BeforeCombatStart()
	{
		await Dig();
	}

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner && Owner.PlayerCombatState?.TurnNumber != 1)
		{
			await Dig();
		}
	}

	private async Task Dig()
	{
		Flash();
		TrumpVfx.Shovel(Owner.Creature);
		await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Build"].BaseValue);
	}
}
