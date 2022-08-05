# Photo Wall Generator

A Unity plugin to generate a grid of photos from your favourite VR game.

## Usage

1. Install this `.unitypackage`
2. Install ImageMagick (from [here](https://imagemagick.org/script/download.php#windows) select "ImageMagick-7.1.0-45-Q16-HDRI-x64-dll.exe" or newer)
3. In Unity open PeanutTools -> Photo Wall Generator
4. Select the folder containing your photos (1080p PNG only)
5. Select a prefab to use per photo
6. Change the row and column settings to match your world
7. Run!

Your prefab needs to have an immediate child named `Photo` that contains some kind of renderer.
