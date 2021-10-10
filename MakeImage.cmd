@echo off

setlocal

if not exist Animation mkdir Animation

echo --- Create source PNGs ---

for %%a in (output_frame_*.png) do if not exist Animation\%%a (
    echo Working on %%a
    cimg %%a -resize 1920x1080 Animation\%%a
)

