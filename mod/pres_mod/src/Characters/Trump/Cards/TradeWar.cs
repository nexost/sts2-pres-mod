using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

public sealed class TradeWar : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(5m, ValueProp.Move),
		new PowerVar<TariffPower>(2m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tariff };

	public TradeWar()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_rock_shatter", null, "blunt_attack.mp3")
			.Execute(choiceContext);
		foreach (Creature enemy in CombatState!.HittableEnemies.ToList())
		{
			await PowerCmd.Apply<TariffPower>(choiceContext, enemy, DynamicVars[nameof(TariffPower)].BaseValue, Owner.Creature, this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars[nameof(TariffPower)].UpgradeValueBy(1m);
	}
}
