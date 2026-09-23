using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TrumpMod.Mechanics;

namespace TrumpMod.Models.Powers;

/// <summary>
/// Highest Wall stage reached this combat (1 fence, 2 brick, 3 concrete, 4 Big Beautiful Wall). Only ever goes up,
/// so the perks stay after a Demolition. Also carries the per-turn Section effects:
///   end of turn: +3 Block per Section; stage 4: 1 damage to ALL enemies per Section.
///   stage 2+: draw 1 extra card each turn. stage 3+: +1 Energy each turn.
/// </summary>
public sealed class WallStagePower : PowerModel
{
	private const string _perksVar = "Perks";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new StringVar(_perksVar) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		RefreshPerkText();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power == this)
		{
			RefreshPerkText();
		}
		return Task.CompletedTask;
	}

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}
		int sections = WallCmd.GetSections(Owner);
		if (sections <= 0)
		{
			return;
		}
		decimal perSection = WallRules.BlockPerSection;
		foreach (ISectionBlockModifier modifier in ModHooks.ListenersOf<ISectionBlockModifier>(Owner))
		{
			perSection = modifier.ModifySectionBlock(Owner, perSection);
		}
		Flash();
		await CreatureCmd.GainBlock(Owner, sections * perSection, ValueProp.Unpowered, null);
		if (Amount >= WallRules.BigBeautifulStage && CombatState.HittableEnemies.Any())
		{
			await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, sections * WallRules.StageDamagePerSection, ValueProp.Unpowered, Owner, null);
		}
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner.Player && Amount >= WallRules.BrickStage ? count + 1 : count;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner.Player && Amount >= WallRules.ConcreteStage ? amount + 1 : amount;
	}

	private void RefreshPerkText()
	{
		var lines = new List<string>();
		for (int stage = 1; stage <= System.Math.Min(Amount, WallRules.BigBeautifulStage); stage++)
		{
			lines.Add(new LocString("powers", $"{Id.Entry}.stage{stage}").GetFormattedText());
		}
		((StringVar)DynamicVars[_perksVar]).StringValue = string.Join("\n", lines);
	}
}
