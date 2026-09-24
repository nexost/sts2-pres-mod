using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Powers;

/// <summary>
/// The Wall's height. Grows with Build, never takes damage, drops only when spent (Demolition...).
/// Section Block and stage perks live on <see cref="WallStagePower"/> so they survive the Wall being torn down.
/// </summary>
public sealed class WallPower : PowerModel
{
	private const string _sectionsVar = "Sections";

	private const string _nextVar = "NextStage";

	private const string _blockVar = "SectionBlock";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(_sectionsVar, 0m),
		new DynamicVar(_blockVar, 0m),
		new DynamicVar(_nextVar, WallRules.StageHeights[0])
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section, HoverTipFactory.Static(StaticHoverTip.Block) };

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		RefreshVars();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power == this)
		{
			RefreshVars();
		}
		return Task.CompletedTask;
	}

	private void RefreshVars()
	{
		DynamicVars[_sectionsVar].BaseValue = WallRules.SectionsFor(Amount);
		DynamicVars[_blockVar].BaseValue = WallRules.SectionsFor(Amount) * WallRules.BlockPerSection;
		DynamicVars[_nextVar].BaseValue = WallRules.NextStageHeight(WallRules.StageFor(Amount)) ?? 0;
	}
}
