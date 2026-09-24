using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) Weak, (2) Vulnerable, (3) Block.</summary>
public sealed class TalkingPoints : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<WeakPower>(2m),
		new PowerVar<VulnerablePower>(2m),
		new BlockVar(8m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips
		.Append(HoverTipFactory.FromPower<WeakPower>()).Append(HoverTipFactory.FromPower<VulnerablePower>());

	public TalkingPoints()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars[nameof(WeakPower)].BaseValue, Owner.Creature, this);
				break;
			case 2:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars[nameof(VulnerablePower)].BaseValue, Owner.Creature, this);
				break;
			case 3:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(WeakPower)].UpgradeValueBy(1m);
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
