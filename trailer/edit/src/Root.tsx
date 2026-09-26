import React from 'react';
import {Composition} from 'remotion';
import {T2_DURATION, T2Test} from './T2Test';
import {Animatic, animaticDuration} from './Animatic';
import {TitleDraft} from './TitleDraft';
import {Banner} from './trailer/Banner';
import {Thumbnail} from './trailer/Thumbnail';
import {Trailer} from './trailer/Trailer';
import {DURATION} from './trailer/data';

// Master format: 2560x1440 at 60 fps (docs/00_project_plan.md, Step 12).
export const WIDTH = 2560;
export const HEIGHT = 1440;
export const FPS = 60;

export const Root: React.FC = () => {
	return (
		<>
			<Composition id="T2Test" component={T2Test} durationInFrames={T2_DURATION} fps={FPS} width={WIDTH} height={HEIGHT} />
			{/* Review renders at 1080p30: storyboard stills don't need 60 fps. */}
			<Composition id="Animatic" component={Animatic} durationInFrames={animaticDuration(30)} fps={30} width={1920} height={1080} />
			{/* T5 title drafts, rendered as stills: npx remotion still src/index.ts TitleDraft out/x.png --props=... */}
			<Composition id="TitleDraft" component={TitleDraft} durationInFrames={1} fps={FPS} width={WIDTH} height={HEIGHT} defaultProps={{variant: 'spectral' as const, art: 'keyart/keyart_walk_s22.png'}} />
			{/* The trailer (T6). Previews: --scale=0.5. captions: burned-in narration subtitles (for muted autoplay). */}
			<Composition id="Trailer" component={Trailer} durationInFrames={Math.round(DURATION * FPS)} fps={FPS} width={WIDTH} height={HEIGHT} defaultProps={{captions: false}} />
			{/* The thumbnail (T7), a still: npx remotion still src/index.ts Thumbnail out/thumbnail.png */}
			<Composition id="Thumbnail" component={Thumbnail} durationInFrames={1} fps={FPS} width={1920} height={1080} defaultProps={{art: 'keyart/keyart_backtoback_s11.png'}} />
			{/* The README's banner, a still: npx remotion still src/index.ts Banner out/banner.png */}
			<Composition id="Banner" component={Banner} durationInFrames={1} fps={FPS} width={2400} height={900} defaultProps={{art: 'keyart/keyart_walk_s22.png'}} />
		</>
	);
};
