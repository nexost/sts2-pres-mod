// The collection beat (54.0-63.0): a 3D wave of both decks, four hero cards slammed one per hit (each chased by half a
// second of it in play, which is a normal shot underneath), then the relics and potions ringing both presidents while
// the counters roll up. Driven by the "collection" graphic in timeline.json.
import React, {useEffect, useState} from 'react';
import {AbsoluteFill, Easing, Img, continueRender, delayRender, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {FONTS} from '../fonts';
import {Graphic, cardUrl, clamp01, rand} from './data';
import {foil} from './Overlays';

type Entry = {id: string; name: string; rarity?: string};
type Collection = Record<'trump' | 'biden', Record<'cards' | 'relics' | 'potions', Entry[]>>;

// Model id -> file name: SlapATariff -> slap_a_tariff.
const snake = (id: string) => id.replace(/([a-z0-9])([A-Z])/g, '$1_$2').replace(/([A-Z])([A-Z][a-z])/g, '$1_$2').toLowerCase();

const useCollection = () => {
	const [data, setData] = useState<Collection | null>(null);
	const [handle] = useState(() => delayRender('collection.json'));
	useEffect(() => {
		fetch(staticFile('collection.json'))
			.then((r) => r.json())
			.then((d) => {
				setData(d);
				continueRender(handle);
			});
	}, [handle]);
	return data;
};

/** The wave's cards: the rarer ones first, spread through each deck. */
const pick = (cards: Entry[], n: number) => {
	const pool = cards.filter((c) => c.rarity && !['Basic', 'Token'].includes(c.rarity));
	const ranked = [...pool.filter((c) => c.rarity === 'Rare'), ...pool.filter((c) => c.rarity !== 'Rare')];
	return ranked.slice(0, n).map((c) => snake(c.id));
};

const Backdrop: React.FC<{t: number; tint: string}> = ({t, tint}) => (
	<AbsoluteFill style={{background: `radial-gradient(ellipse at 50% 45%, ${tint} 0%, #0b0907 55%, #030303 100%)`}}>
		<AbsoluteFill
			style={{
				background: 'repeating-conic-gradient(from 0deg at 50% 45%, rgba(255,220,150,0.07) 0deg 4deg, rgba(0,0,0,0) 4deg 14deg)',
				transform: `rotate(${t * 6}deg) scale(2)`,
				opacity: 0.8,
			}}
		/>
	</AbsoluteFill>
);

const Wave: React.FC<{t: number; start: number; data: Collection}> = ({t, start, data}) => {
	const {width, height} = useVideoConfig();
	const n = 34;
	const decks = {trump: pick(data.trump.cards, n), biden: pick(data.biden.cards, n)};
	const items = [];
	for (let j = 0; j < n; j++) {
		for (const [k, who] of (['trump', 'biden'] as const).entries()) {
			const seed = j * 2 + k;
			// Two streams crossing: Donald's deck left to right in the upper half, Joe's right to left in the lower half,
			// dense until the first hero card slams in.
			const t0 = start - 0.35 + j * 0.07 + k * 0.035;
			const p = (t - t0) / 0.95;
			if (p < 0 || p > 1) continue;
			const dir = who === 'trump' ? 1 : -1;
			const x = interpolate(p, [0, 1], [-0.95, 0.95]) * width * dir;
			const y = (rand(seed) * 0.5 + (who === 'trump' ? -0.5 : 0.02)) * height * 0.9 + interpolate(p, [0, 1], [0.06, -0.06]) * height * dir;
			const z = interpolate(rand(seed + 50), [0, 1], [-1500, -150]);
			const h = height * 0.4;
			items.push(
				<Img
					key={seed}
					src={cardUrl(`${who}/${decks[who][j]}`)}
					style={{
						position: 'absolute',
						left: width / 2 - h * 0.368,
						top: height / 2 - h / 2,
						height: h,
						transform: `translate3d(${x}px, ${y}px, ${z}px) rotateY(${interpolate(p, [0, 1], [55, -55]) * dir}deg) rotateZ(${(rand(seed + 9) - 0.5) * 30 + p * 20 * dir}deg)`,
						filter: 'drop-shadow(0 20px 30px rgba(0,0,0,0.7))',
					}}
				/>,
			);
		}
	}
	return <AbsoluteFill style={{perspective: 1600, transformStyle: 'preserve-3d'}}>{items}</AbsoluteFill>;
};

const Slam: React.FC<{hero: Graphic; t: number}> = ({hero, t}) => {
	const {fps, width, height} = useVideoConfig();
	const f = (t - hero.at) * fps;
	const s = spring({frame: f, fps, config: {damping: 14, stiffness: 240, mass: 0.7}});
	const land = interpolate(f, [5, 7, 20], [0, 1, 0], clamp01);
	const ring = interpolate(f, [6, 22], [0, 1], {...clamp01, easing: Easing.out(Easing.cubic)});
	const nameIn = spring({frame: f - 6, fps, config: {damping: 12, stiffness: 260, mass: 0.6}});
	const trump = (hero.card as string).startsWith('trump');
	// While it holds: a slow push-in and one light sweep, so the card can be read without going static.
	const hold = (hero.hold ?? 0.5) * fps;
	const drift = interpolate(f, [8, hold], [0, 1], {...clamp01, easing: Easing.out(Easing.quad)});
	const shine = interpolate(f, [10, Math.max(11, hold - 6)], [-20, 125], clamp01);
	return (
		<AbsoluteFill>
			<Backdrop t={t} tint={trump ? '#5a3a10' : '#18265e'} />
			<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
				<div style={{position: 'absolute', width: height * 1.4 * ring, height: height * 1.4 * ring, borderRadius: '50%', border: `${10 * (1 - ring) + 2}px solid ${trump ? 'rgba(255,210,120,0.8)' : 'rgba(150,180,255,0.8)'}`, opacity: 1 - ring}} />
			</AbsoluteFill>
			<AbsoluteFill style={{perspective: 1400, justifyContent: 'center', alignItems: 'center'}}>
				<div
					style={{
						position: 'relative',
						height: height * 0.66,
						marginTop: -height * 0.08,
						transform: `translateZ(${interpolate(s, [0, 1], [-2600, 0])}px) rotateX(${interpolate(s, [0, 1], [48, 0])}deg) rotateZ(${interpolate(s, [0, 1], [-14, -2]) + drift * 1.5}deg) scale(${1 + drift * 0.07})`,
						filter: `drop-shadow(0 24px 40px rgba(0,0,0,0.8)) drop-shadow(0 0 ${60 * land + 20}px ${trump ? 'rgba(255,200,90,0.8)' : 'rgba(120,160,255,0.8)'})`,
					}}
				>
					<Img src={cardUrl(hero.card)} style={{height: '100%', display: 'block'}} />
					{/* A light sweep across the card while it holds, clipped to the card's own shape. */}
					<div
						style={{
							position: 'absolute',
							inset: 0,
							WebkitMaskImage: `url(${cardUrl(hero.card)})`,
							WebkitMaskSize: '100% 100%',
							maskImage: `url(${cardUrl(hero.card)})`,
							maskSize: '100% 100%',
							background: `linear-gradient(115deg, rgba(255,255,255,0) ${shine - 14}%, rgba(255,250,230,0.55) ${shine}%, rgba(255,255,255,0) ${shine + 14}%)`,
							mixBlendMode: 'screen',
						}}
					/>
				</div>
			</AbsoluteFill>
			<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'center', paddingBottom: height * 0.06}}>
				<div style={{fontFamily: FONTS.spectral.family, ...foil(width * 0.042), letterSpacing: width * 0.004, transform: `scale(${interpolate(nameIn, [0, 1], [1.6, 1])})`, opacity: Math.min(1, nameIn * 2), filter: 'drop-shadow(0 4px 0 rgba(40,18,0,0.9))'}}>
					{hero.name}
				</div>
			</AbsoluteFill>
			<AbsoluteFill style={{backgroundColor: 'white', opacity: land * 0.35}} />
		</AbsoluteFill>
	);
};

/** After its slam, the hero card rides in the corner over its gameplay. */
const Corner: React.FC<{hero: Graphic; t: number}> = ({hero, t}) => {
	const {fps, width, height} = useVideoConfig();
	const s = spring({frame: (t - hero.at - (hero.hold ?? 0.5)) * fps, fps, config: {damping: 16, stiffness: 220}});
	return (
		<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start', padding: `${height * 0.05}px ${width * 0.035}px`}}>
			<Img src={cardUrl(hero.card)} style={{height: height * 0.36, transform: `rotate(-4deg) scale(${interpolate(s, [0, 1], [1.6, 1])})`, transformOrigin: 'left bottom', filter: 'drop-shadow(0 12px 20px rgba(0,0,0,0.8))'}} />
		</AbsoluteFill>
	);
};

const Ring: React.FC<{t: number; start: number; dur: number; data: Collection}> = ({t, start, dur, data}) => {
	const {fps, width, height} = useVideoConfig();
	const icons = [
		...data.trump.relics.map((r) => `relics/${snake(r.id)}`),
		...data.biden.relics.map((r) => `relics/${snake(r.id)}`),
		...data.trump.potions.map((p) => `potions/${snake(p.id)}`),
		...data.biden.potions.map((p) => `potions/${snake(p.id)}`),
	];
	// Alternate the two presidents' items around the ring.
	const order = icons.map((_, i) => (i % 2 === 0 ? icons[i / 2] : icons[icons.length - 1 - (i - 1) / 2]));
	const spin = (t - start) * 0.18;
	const sprites = spring({frame: (t - start) * fps, fps, config: {damping: 16, stiffness: 120}});
	const cards = data.trump.cards.filter((c) => c.rarity !== 'Token').length + data.biden.cards.filter((c) => c.rarity !== 'Token').length;
	const relics = data.trump.relics.length + data.biden.relics.length;
	const potions = data.trump.potions.length + data.biden.potions.length;
	const roll = interpolate(t, [start + 0.25, start + 1.2], [0, 1], {...clamp01, easing: Easing.out(Easing.cubic)});
	const num = (n: number) => <span style={{color: '#f3c65a'}}>{Math.round(n * roll)}</span>;
	// A slow push-in over the hold.
	const push = interpolate(t - start, [0, dur], [1, 1.08], {...clamp01, easing: Easing.inOut(Easing.quad)});
	return (
		<AbsoluteFill>
			<Backdrop t={t} tint="#3a2a18" />
			<AbsoluteFill style={{transform: `scale(${push})`, transformOrigin: '50% 44%'}}>
				<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center', flexDirection: 'row', gap: width * 0.01, paddingBottom: height * 0.1}}>
					<Img src={staticFile('ui/sprite_trump.png')} style={{height: height * 0.5, transform: `scaleX(-1) translateX(${(1 - sprites) * 200}px)`, opacity: sprites, filter: 'drop-shadow(0 0 30px rgba(255,200,90,0.35))'}} />
					<Img src={staticFile('ui/sprite_biden.png')} style={{height: height * 0.47, transform: `translateX(${(1 - sprites) * 200}px)`, opacity: sprites, filter: 'drop-shadow(0 0 30px rgba(120,160,255,0.35))'}} />
				</AbsoluteFill>
				{order.map((icon, k) => {
					const a = (k / order.length) * Math.PI * 2 + spin - Math.PI / 2;
					const land = start + 0.08 + k * 0.045;
					const drop = spring({frame: (t - land) * fps, fps, config: {damping: 11, stiffness: 200, mass: 0.6}});
					// Each item glints as the sweep passes it, twice around the ring over the hold.
					const glint = Math.max(
						interpolate(t, [land + 0.9, land + 1.0, land + 1.25], [0, 1, 0], clamp01),
						interpolate(t, [land + 2.3, land + 2.4, land + 2.65], [0, 1, 0], clamp01),
					);
					const size = height * 0.1;
					const x = width / 2 + Math.cos(a) * width * 0.36 - size / 2;
					const y = height * 0.44 + Math.sin(a) * height * 0.36 - size / 2 - (1 - drop) * height * 0.4;
					return (
						<Img
							key={icon}
							src={staticFile(`${icon}.png`)}
							style={{position: 'absolute', left: x, top: y, width: size, height: size, opacity: t >= land ? 1 : 0, filter: `drop-shadow(0 6px 10px rgba(0,0,0,0.7)) drop-shadow(0 0 ${6 + glint * 30}px rgba(255,235,180,${0.3 + glint * 0.7})) brightness(${1 + glint * 0.6})`}}
						/>
					);
				})}
			</AbsoluteFill>
			<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'center', paddingBottom: height * 0.07}}>
				<div style={{fontFamily: FONTS.kreon.family, fontSize: width * 0.03, letterSpacing: width * 0.003, color: '#f4ecd8', textShadow: '0 3px 12px rgba(0,0,0,0.95)', opacity: roll > 0 ? 1 : 0}}>
					{num(cards)} CARDS · {num(relics)} RELICS · {num(potions)} POTIONS · CO-OP
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};

export const CollectionBeat: React.FC<{g: Graphic}> = ({g}) => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const data = useCollection();
	if (!data) return null;
	const t = g.at + frame / fps;
	const heroes = g.heroes as Graphic[];
	if (t < g.wave.at + g.wave.dur) {
		return (
			<AbsoluteFill>
				<Backdrop t={t} tint="#2c2418" />
				<Wave t={t} start={g.wave.at} data={data} />
			</AbsoluteFill>
		);
	}
	if (t >= g.ring.at) {
		return <Ring t={t} start={g.ring.at} dur={g.ring.dur} data={data} />;
	}
	const current = [...heroes].reverse().find((h) => t >= h.at);
	if (!current) return null;
	return t < current.at + (current.hold ?? 0.5) ? <Slam hero={current} t={t} /> : <Corner hero={current} t={t} />;
};
