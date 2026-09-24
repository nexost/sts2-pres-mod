using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Rampage.</summary>
public sealed class Bigly : CardModel
{
	private decimal _extraDamageFromPlays;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(11m, ValueProp.Move),
		new DynamicVar(CardVars.Increase, 6m)
	};

	/// <summary>Kept so the extra damage survives a downgrade (Magiknight), like Rampage.</summary>
	private decimal ExtraDamageFromPlays
	{
		get
		{
			return _extraDamageFromPlays;
		}
		set
		{
			AssertMutable();
			_extraDamageFromPlays = value;
		}
	}

	public Bigly()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);
		DynamicVars.Damage.BaseValue += DynamicVars[CardVars.Increase].BaseValue;
		ExtraDamageFromPlays += DynamicVars[CardVars.Increase].BaseValue;
	}

	protected override void AfterDowngraded()
	{
		base.AfterDowngraded();
		DynamicVars.Damage.BaseValue += ExtraDamageFromPlays;
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		DynamicVars[CardVars.Increase].UpgradeValueBy(2m);
	}
}
