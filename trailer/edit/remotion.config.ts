// Remotion settings for the trailer edit (docs/00_project_plan.md, Step 12).
// Media (chosen takes, music, voice, AI shots) live in build/trailer/media/, git-ignored; staticFile() paths start there.
import {Config} from '@remotion/cli/config';

Config.setPublicDir('../../build/trailer/media');
Config.setVideoImageFormat('jpeg');
Config.setJpegQuality(95);
Config.setCodec('h264');
// CRF is given per render (--crf=14 for reviews): the ProRes master rejects any CRF setting.
Config.setPixelFormat('yuv420p');
