using MegaCrit.Sts2.Core.Saves.Runs;

namespace TrumpMod.Models.Cards;

/// <summary>
/// Run-long scaling like Feed and Genetic Algorithm: each Fatal hit permanently raises this card's Build, on the deck copy too,
/// so the Wall starts higher every fight. The grown value is a saved property (registered in ModEntry).
/// </summary>
public sealed class Cornerstone : CardModel
{
	private const int _baseBuild = 3;

	private int _currentBuild = _baseBuild;

	private int _increasedBuild;

	public override bool CanBeGeneratedInCombat => false;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Innate };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(8m, ValueProp.Move),
		new DynamicVar(CardVars.Build, CurrentBuild),
		new DynamicVar(CardVars.Increase, 3m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, HoverTipFactory.Static(StaticHoverTip.Fatal) };

	[SavedProperty]
	public int CurrentBuild
	{
		get
		{
			return _currentBuild;
		}
		set
		{
			AssertMutable();
			_currentBuild = value;
			DynamicVars[CardVars.Build].BaseValue = _currentBuild;
		}
	}

	[SavedProperty]
	public int IncreasedBuild
	{
		get
		{
			return _increasedBuild;
		}
		set
		{
			AssertMutable();
			_increasedBuild = value;
		}
	}

	public Cornerstone()
		: base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
		AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
		await WallCmd.Build(choiceContext, Owner.Creature, DynamicVars[CardVars.Build].BaseValue, this);
		if (shouldTriggerFatal && attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled))
		{
			int increase = DynamicVars[CardVars.Increase].IntValue;
			BuffFromKill(increase);
			(DeckVersion as Cornerstone)?.BuffFromKill(increase);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}

	protected override void AfterDowngraded()
	{
		UpdateBuild();
	}

	private void BuffFromKill(int extraBuild)
	{
		IncreasedBuild += extraBuild;
		UpdateBuild();
	}

	private void UpdateBuild()
	{
		CurrentBuild = _baseBuild + IncreasedBuild;
	}
}
