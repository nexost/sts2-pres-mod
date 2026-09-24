namespace PresMod.Characters.Biden.Cards;

/// <summary>Starter Tangent: (1) damage, (2) Block, (3) draw 2. As Dark Brandon, all three.</summary>
public sealed class HeresTheDeal : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(8m, ValueProp.Move),
		new BlockVar(7m, ValueProp.Move),
		new CardsVar(2)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block));

	public HeresTheDeal()
		: base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_attack_blunt")
					.Execute(choiceContext);
				break;
			case 2:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
			case 3:
				await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
