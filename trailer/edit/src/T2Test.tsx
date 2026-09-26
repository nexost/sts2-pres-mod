// T2 pipeline test: one captured clip cut with the trailer's tools (push-in, speed ramp, flash, shake, title slam).
import React from 'react';
import {AbsoluteFill, Easing, OffthreadVideo, Sequence, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';

const FPS = 60;
const sec = (s: number) => Math.round(s * FPS);

// Source times in the clip (seconds): the wake burst lands at 2.9, the full beam at 3.4.
const SEGMENTS = [
	{from: 0.6, to: 2.85, rate: 1},
	{from: 2.85, to: 3.45, rate: 0.35},
	{from: 3.45, to: 5.4, rate: 1},
];

const segmentFrames = SEGMENTS.map((s) => sec((s.to - s.from) / s.rate));
export const T2_DURATION = segmentFrames.reduce((a, b) => a + b, 0);
const SLOW_START = segmentFrames[0];
const HIT = segmentFrames[0] + segmentFrames[1];

const Clip: React.FC = () => {
	let start = 0;
	return (
		<>
			{SEGMENTS.map((s, i) => {
				const from = start;
				start += segmentFrames[i];
				return (
					<Sequence key={i} from={from} durationInFrames={segmentFrames[i]}>
						<OffthreadVideo src={staticFile('clips/biden_wake_t2.mp4')} trimBefore={sec(s.from)} playbackRate={s.rate} muted style={{width: '100%', height: '100%'}} />
					</Sequence>
				);
			})}
		</>
	);
};

// Deterministic shake: decaying sine noise from the hit.
const shakeAt = (frame: number) => {
	const t = frame - HIT;
	if (t < 0 || t > sec(1.1)) return {x: 0, y: 0};
	const decay = Math.exp(-t / 14);
	return {x: Math.sin(t * 2.3) * 26 * decay, y: Math.cos(t * 3.1) * 18 * decay};
};

export const T2Test: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();

	const pushIn = interpolate(frame, [0, SLOW_START, HIT], [1.0, 1.28, 1.36], {extrapolateRight: 'clamp', easing: Easing.inOut(Easing.cubic)});
	const snapOut = interpolate(frame, [HIT, HIT + 9], [1.36, 1.04], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp', easing: Easing.out(Easing.cubic)});
	const zoom = frame < HIT ? pushIn : snapOut;
	const shake = shakeAt(frame);
	const flash = interpolate(frame, [HIT, HIT + 2, HIT + 12], [0, 0.85, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
	const redWash = interpolate(frame, [SLOW_START, HIT, HIT + 40], [0, 0.18, 0.08], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});

	const titleIn = spring({frame: frame - (HIT + 6), fps, config: {damping: 11, stiffness: 180, mass: 0.7}});
	const titleScale = interpolate(titleIn, [0, 1], [1.9, 1]);
	const titleOpacity = interpolate(frame, [HIT + 6, HIT + 10], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
	const fontSize = width * 0.085;

	return (
		<AbsoluteFill style={{backgroundColor: 'black', overflow: 'hidden'}}>
			<AbsoluteFill style={{transform: `translate(${shake.x}px, ${shake.y}px) scale(${zoom})`, transformOrigin: '26% 56%'}}>
				<Clip />
			</AbsoluteFill>
			<AbsoluteFill style={{background: 'radial-gradient(ellipse at 40% 50%, rgba(0,0,0,0) 45%, rgba(0,0,0,0.55) 100%)'}} />
			<AbsoluteFill style={{backgroundColor: `rgba(200, 20, 30, ${redWash})`, mixBlendMode: 'multiply'}} />
			<AbsoluteFill style={{backgroundColor: 'white', opacity: flash}} />
			<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
				<div
					style={{
						fontFamily: 'Impact, "Arial Black", sans-serif',
						fontSize,
						letterSpacing: fontSize * 0.04,
						color: '#ff2d2d',
						opacity: titleOpacity,
						transform: `translateY(${width * 0.12}px) scale(${titleScale})`,
						textShadow: `-6px 0 0 rgba(0,255,255,0.35), 6px 0 0 rgba(255,0,60,0.6), 0 0 40px rgba(255,30,30,0.9), 0 0 110px rgba(255,0,0,0.6)`,
					}}
				>
					DARK BRANDON
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
