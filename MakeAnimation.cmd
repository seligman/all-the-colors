@echo off

setlocal

if not exist Animation mkdir Animation

echo --- Create source PNGs ---

for %%a in (output_frame_*.png) do if not exist Animation\%%a (
    echo Working on %%a
    cimg %%a -resize 1920x1080 Animation\%%a
)

cd Animation

if exist frames.* del frames.*
if exist video.* del video.*

echo --- Create Y4M file ---

ffmpeg -framerate 60 -i "output_frame_%%04d.png" -pix_fmt yuv420p frames.y4m

set XOPT=--profile high --level 4 --preset placebo --tune film --bitrate 10000 --stats video.stats
set XOPT=%XOPT% --keyint 70 --min-keyint 28 --bframes 2 --b-adapt 2 --ref 4 --vbv-bufsize 25000
set XOPT=%XOPT% --vbv-maxrate 25000 --sar 1:1 --aud --nal-hrd vbr --range auto --pic-struct
set XOPT=%XOPT% --colorprim bt709 --transfer bt709 --colormatrix bt709 --no-scenecut --threads 16 --force-cfr

echo --- x264 encode  ---

x264 %XOPT% --pass 1 --output NUL frames.y4m
x264 %XOPT% --pass 1 --pass 2 --output NUL frames.y4m
x264 %XOPT% --pass 1 --pass 3 --output video.264 frames.y4m

ffmpeg -r 60 -i video.264 -vcodec copy -movflags faststart video.mp4

echo --- Done ---

dir video.mp4
