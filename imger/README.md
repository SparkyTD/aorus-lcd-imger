## GIGABYTE AORUS LCD GIF CONVERTER
Gigabyte Aorus MASTER RTX 3070

### Usage:
```
imgur.exe input targetW targetH srcX srcY srcW srcH output depth bpp compress

    input               The input GIF file
    targetW, targetH    Size of the output frames
    srcX, srcY          Upper-left corner of crop area
    srcW, srcH          Size of crop area
    output              Path where the animation.ini will be saved
    depth               ???
    bpp                 Bits per pixel (e.g. 16 = PixelFormat.Format16bppRgb565)
    compress            Compression algorithm
``` 

### Example:
```
imger.exe "C:\Users\Sparky\Downloads\vibing cat.gif" 320 170 0 0 320 170 \
    "C:\Users\Sparky\RiderProjects\RGBFusion\imger\bin\Debug\assets\animation.ini" 10 16 RLE
```

The program generates a file called animation.bin in the output folder. 
This file contains the RLE-compressed image data of the GIF frames. 
This is the layout of the file:

|    Offset   |   Length  |    Type   |              Description             |
|:-----------:|:---------:|:---------:|:------------------------------------:|
|      0      |     2     |   ushort  |    Total number of frames (nFrame)   |
|      2      | 10*nFrame | FrameInfo | Data block that describes the frames |
| 2+10*nFrame |     -     |   Frame   |       RLE-Compressed frame data      |

FrameInfo:

| Offset | Length |  Type  |       Description       |
|:------:|:------:|:------:|:-----------------------:|
|    0   |    4   |  uint  |   Offset of frame data  |
|    4   |    2   | ushort |     Width of a frame    |
|    6   |    2   | ushort |    Height of a frame    |
|    8   |    2   | ushort | Image Format (always 3) |