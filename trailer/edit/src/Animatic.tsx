// The animatic (T3): the audio cut (build/trailer/media/mix.wav) over the storyboard frames, with the name cards,
// subtitles and a timecode, to judge pacing before any footage exists. Everything comes from timeline.json.
import React from 'react';
import {AbsoluteFill, Audio, Img, Sequence, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import timeline from './timeline.json';
import markers from './vo_markers.json';

type Card = {text: string; style: string};
type Beat = {at: number; dur: number; frame: string | null; section: string; shot: string; card?: Card};

const BEATS = timeline.beats as Beat[];
const FLASH_ON = new Set(['impact', 'braam', 'logo_hit', 'stamp', 'sub_drop']);

export const animaticDuration = (fps: number) => Math.round(timeline.duration * fps);

const Still: React.FC<{src: string; frames: number}> = ({src, frames}) => {
	const f = useCurrentFrame();
	const scale = interpolate(f, [0, frames], [1.0, 1.07]);
	return <Img src={staticFile(`storyboard/${src}`)} style={{width: '100%', height: '100%', objectFit: 'cover', transform: `scale(${scale})`}} />;
};

const NameCard: React.FC<Card> = ({text, style}) => {
	const f = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const s = spring({frame: f - 3, fps, config: {damping: 12, stiffness: 170}});
	const size = style === 'stats' ? width * 0.03 : width * 0.075;
	const look: Record<string, React.CSSProperties> = {
		donald: {fontFamily: 'Georgia, serif', fontWeight: 700, color: '#f3d27a', textShadow: '0 4px 0 #6b4a10, 0 0 30px rgba(230,184,74,0.7)', letterSpacing: size * 0.06},
		joe: {fontFamily: 'Georgia, serif', fontWeight: 700, color: '#aeb7ff', textShadow: '0 4px 0 #1b2060, 0 0 30px rgba(110,120,255,0.6)', letterSpacing: size * 0.06},
		brandon: {fontFamily: 'Impact, sans-serif', color: '#ff2d2d', textShadow: '-5px 0 0 rgba(0,255,255,0.35), 5px 0 0 rgba(255,0,60,0.6), 0 0 40px rgba(255,30,30,0.9)', letterSpacing: size * 0.04},
		stats: {fontFamily: 'Impact, sans-serif', color: '#ffffff', letterSpacing: size * 0.12, textShadow: '0 0 24px rgba(0,0,0,0.9)'},
	};
	return (
		<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
			<div style={{fontSize: size, transform: `scale(${interpolate(s, [0, 1], [1.6, 1])})`, opacity: Math.min(1, s * 1.5), ...look[style]}}>
				{style === 'joe' ? text.split('').map((c, i) => (
					<span key={i} style={{display: 'inline-block', transform: `translateY(${Math.max(0, f - 20) * 0.35 * ((i % 3) + 1)}px) rotate(${Math.max(0, f - 20) * 0.12 * ((i % 2) ? 1 : -1)}deg)`}}>{c === ' ' ? ' ' : c}</span>
				)) : text}
			</div>
		</AbsoluteFill>
	);
};

const TitleCard: React.FC = () => {
	const f = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const s = spring({frame: f, fps, config: {damping: 14, stiffness: 120}});
	return (
		<AbsoluteFill>
			<AbsoluteFill style={{flexDirection: 'row'}}>
				<Img src={staticFile('storyboard/key_donald.jpg')} style={{width: '50%', height: '100%', objectFit: 'cover', filter: 'brightness(0.5)'}} />
				<Img src={staticFile('storyboard/key_joe.jpg')} style={{width: '50%', height: '100%', objectFit: 'cover', filter: 'brightness(0.5)'}} />
			</AbsoluteFill>
			<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: width * 0.01}}>
				<div style={{fontFamily: 'Georgia, serif', fontWeight: 700, fontSize: width * 0.05, color: '#f7e7b3', letterSpacing: width * 0.004, transform: `scale(${interpolate(s, [0, 1], [1.25, 1])})`, textShadow: '0 5px 0 #6b4a10, 0 0 40px rgba(230,184,74,0.6)'}}>PRESIDENTS OF THE SPIRE</div>
				<div style={{fontFamily: 'Arial, sans-serif', fontWeight: 700, fontSize: width * 0.016, color: '#ece6d5', letterSpacing: width * 0.003, opacity: interpolate(f, [20, 35], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})}}>A FREE MOD FOR SLAY THE SPIRE 2</div>
				<div style={{fontFamily: 'Arial, sans-serif', fontSize: width * 0.013, color: '#c9c2b0', opacity: interpolate(f, [35, 50], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})}}>Download free · link in the description · github.com/nexost/sts2-pres-mod</div>
			</AbsoluteFill>
			<div style={{position: 'absolute', bottom: width * 0.012, width: '100%', textAlign: 'center', fontFamily: 'Arial, sans-serif', fontSize: width * 0.008, color: '#8d8878'}}>A fan-made mod. Not affiliated with Mega Crit.</div>
		</AbsoluteFill>
	);
};

const ButtonCard: React.FC = () => {
	const f = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const tweet = 'BEST MOD EVER. MANY PEOPLE ARE SAYING.';
	const b = timeline.button;
	const typed = tweet.slice(0, Math.floor(interpolate(f, [b.tweetFrom * fps, b.tweetTo * fps], [0, tweet.length], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})));
	const joe = "Here's the deal, folks. Back in—";
	const joeShown = joe.slice(0, Math.floor(interpolate(f, [b.joeFrom * fps, b.joeTo * fps], [0, joe.length], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})));
	const bubble: React.CSSProperties = {fontFamily: 'Arial, sans-serif', fontWeight: 700, fontSize: width * 0.024, padding: `${width * 0.012}px ${width * 0.018}px`, borderRadius: width * 0.02, maxWidth: '62%'};
	return (
		<AbsoluteFill style={{backgroundColor: 'black', justifyContent: 'center', padding: '0 12%', gap: width * 0.02, flexDirection: 'column'}}>
			{typed && <div style={{...bubble, background: '#2a5d8f', color: 'white', alignSelf: 'flex-start', borderBottomLeftRadius: 6}}>{typed}</div>}
			{joeShown && <div style={{...bubble, background: '#3b4a58', color: '#e9eef2', fontWeight: 400, alignSelf: 'flex-end', borderBottomRightRadius: 6}}>{joeShown}</div>}
		</AbsoluteFill>
	);
};

export const Animatic: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const t = frame / fps;
	const beat = [...BEATS].reverse().find((b) => t >= b.at) ?? BEATS[0];
	const line = (markers as {at: number; end: number; text: string}[]).find((m) => t >= m.at && t < m.end + 0.25);
	// "Holy shit" hits: on every big sound, a zoom punch, a shake and a few blown-out, deep-fried frames.
	const hit = Math.max(0, ...timeline.sfx.filter((s) => FLASH_ON.has(s.name)).map((s) => {
		const d = t - s.at;
		return d >= 0 && d < 0.35 ? Math.exp(-d / 0.08) * (s.gain >= -3 ? 1 : 0.55) : 0;
	}));
	const flash = hit * 0.28;
	const punch = 1 + hit * 0.09;
	const shakeX = Math.sin(frame * 2.7) * 22 * hit;
	const shakeY = Math.cos(frame * 3.3) * 16 * hit;
	const fried = hit > 0.25 ? `saturate(${1 + hit * 1.8}) contrast(${1 + hit * 0.55}) brightness(${1 + hit * 0.12})` : 'none';
	const tc = `${Math.floor(t / 60)}:${(t % 60).toFixed(1).padStart(4, '0')}`;
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<Audio src={staticFile('mix.wav')} />
			<AbsoluteFill style={{transform: `translate(${shakeX}px, ${shakeY}px) scale(${punch})`, filter: fried}}>
				{BEATS.map((b, i) => (
					<Sequence key={i} from={Math.round(b.at * fps)} durationInFrames={Math.max(1, Math.round(b.dur * fps))}>
						{b.frame === 'title' ? <TitleCard /> : b.frame === 'button' ? <ButtonCard /> : b.frame ? <Still src={b.frame} frames={Math.round(b.dur * fps)} /> : null}
						{b.card && <NameCard {...b.card} />}
					</Sequence>
				))}
			</AbsoluteFill>
			<AbsoluteFill style={{backgroundColor: 'white', opacity: flash}} />
			{line && (
				<div style={{position: 'absolute', bottom: width * 0.055, width: '100%', textAlign: 'center', fontFamily: '"Courier New", monospace', fontWeight: 700, fontSize: width * 0.022, color: '#fff', textShadow: '0 0 8px #000, 0 2px 4px #000'}}>
					{line.text}
				</div>
			)}
			<div style={{position: 'absolute', left: 0, right: 0, bottom: 0, height: width * 0.03, background: 'rgba(0,0,0,0.72)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: `0 ${width * 0.012}px`, fontFamily: 'Consolas, monospace', fontSize: width * 0.011, color: '#cfd8d3'}}>
				<span>ANIMATIC · {beat.section} · {beat.shot}</span>
				<span>{tc}</span>
			</div>
		</AbsoluteFill>
	);
};
