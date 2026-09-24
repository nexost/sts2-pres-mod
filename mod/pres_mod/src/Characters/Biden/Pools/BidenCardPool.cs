using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Biden.Cards;

namespace PresMod.Characters.Biden.Pools;

public sealed class BidenCardPool : CardPoolModel
{
	/// <summary>Also the portrait folder: images/packed/card_portraits/biden/&lt;card&gt;.png</summary>
	public override string Title => "biden";

	public override string EnergyColorName => Biden.energyColorName;

	/// <summary>res://materials/cards/frames/card_frame_biden_mat.tres (a hue shift of the shared frame, character.json frame_hsv).</summary>
	public override string CardFrameMaterialPath => "card_frame_biden";

	public override Color DeckEntryCardColor => new Color("434CCF");

	public override Color EnergyOutlineColor => new Color("252A73");

	public override bool IsColorless => false;

	/// <summary>Every card from characters/biden/design/cards.json, in its order (tokens that only borrow the frame stay out).</summary>
	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<StrikeBiden>(),
			ModelDb.Card<DefendBiden>(),
			ModelDb.Card<Catnap>(),
			ModelDb.Card<HeresTheDeal>(),
			ModelDb.Card<FingerGuns>(),
			ModelDb.Card<RedEye>(),
			ModelDb.Card<AviatorGlint>(),
			ModelDb.Card<Sleepwalk>(),
			ModelDb.Card<GroggyHaymaker>(),
			ModelDb.Card<Snore>(),
			ModelDb.Card<LongStoryShort>(),
			ModelDb.Card<Anyway>(),
			ModelDb.Card<WordSalad>(),
			ModelDb.Card<Malarkey>(),
			ModelDb.Card<FullSteamAhead>(),
			ModelDb.Card<CupOfJoe>(),
			ModelDb.Card<MorningPerson>(),
			ModelDb.Card<RestingMyEyes>(),
			ModelDb.Card<CountingSheep>(),
			ModelDb.Card<WhereWasI>(),
			ModelDb.Card<LetMeFinish>(),
			ModelDb.Card<FolksListen>(),
			ModelDb.Card<ChocolateChip>(),
			ModelDb.Card<ComeOnMan>(),
			ModelDb.Card<MicDrop>(),
			ModelDb.Card<AirForceOne>(),
			ModelDb.Card<RiseAndShine>(),
			ModelDb.Card<PillowFight>(),
			ModelDb.Card<SleepingGiant>(),
			ModelDb.Card<LightsOut>(),
			ModelDb.Card<OffTheCuff>(),
			ModelDb.Card<TalkTheirEarOff>(),
			ModelDb.Card<Malaprop>(),
			ModelDb.Card<TheBeast>(),
			ModelDb.Card<AmtrakExpress>(),
			ModelDb.Card<PutOnTheAviators>(),
			ModelDb.Card<UpAllNight>(),
			ModelDb.Card<StareDown>(),
			ModelDb.Card<GameFace>(),
			ModelDb.Card<FiveMoreMinutes>(),
			ModelDb.Card<SleepIn>(),
			ModelDb.Card<FortyWinks>(),
			ModelDb.Card<PillowFort>(),
			ModelDb.Card<LostMyTrainOfThought>(),
			ModelDb.Card<BrainFreeze>(),
			ModelDb.Card<Filibuster>(),
			ModelDb.Card<TalkingPoints>(),
			ModelDb.Card<SugarRush>(),
			ModelDb.Card<AllAboard>(),
			ModelDb.Card<CorvetteCruise>(),
			ModelDb.Card<ReachAcrossTheAisle>(),
			ModelDb.Card<WideAwake>(),
			ModelDb.Card<LaserFocus>(),
			ModelDb.Card<EarlyRiser>(),
			ModelDb.Card<PowerNap>(),
			ModelDb.Card<Snoring>(),
			ModelDb.Card<StreamOfConsciousness>(),
			ModelDb.Card<LongWinded>(),
			ModelDb.Card<AmtrakJoe>(),
			ModelDb.Card<Bipartisan>(),
			ModelDb.Card<LaserShow>(),
			ModelDb.Card<Dogfight>(),
			ModelDb.Card<OutCold>(),
			ModelDb.Card<Sleepwalker>(),
			ModelDb.Card<RambleOn>(),
			ModelDb.Card<BigDeal>(),
			ModelDb.Card<ZeroToSixty>(),
			ModelDb.Card<Motorcade>(),
			ModelDb.Card<Unleashed>(),
			ModelDb.Card<SecondCup>(),
			ModelDb.Card<Hibernate>(),
			ModelDb.Card<SoundAsleep>(),
			ModelDb.Card<DeepSleep>(),
			ModelDb.Card<RepeatTheLine>(),
			ModelDb.Card<MomentOfClarity>(),
			ModelDb.Card<BuildBackBetter>(),
			ModelDb.Card<IceCreamTruck>(),
			ModelDb.Card<DarkBrandonRises>(),
			ModelDb.Card<NoMalarkey>(),
			ModelDb.Card<DoubleVision>(),
			ModelDb.Card<WellRested>(),
			ModelDb.Card<HeavySleeper>(),
			ModelDb.Card<OffScript>(),
			ModelDb.Card<TallTales>(),
			ModelDb.Card<GaffeMachine>(),
			ModelDb.Card<InfrastructureLaw>(),
			ModelDb.Card<StateOfTheUnion>(),
			ModelDb.Card<SoulOfTheNation>()
		};
	}
}
