using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Relics;

/// <summary>
/// Ancient version of the Golden Shovel, given by Touch of Orobas (TouchOfOrobasPatch): at the start of your turn, Build 4.
/// Starter rarity like Black Blood, so it's never offered as a random reward.
/// </summary>
public sealed class DiamondShovel : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Build", 4m) };

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
