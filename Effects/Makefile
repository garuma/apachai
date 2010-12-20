FILES = BaseEffect.cs InvertColorsEffect.cs UnaryPixelOp.cs FastBitmap.cs BlackAndWhiteEffect.cs UnaryPixelOps.cs SepiaEffect.cs PixelOp.cs EffectAdjustment.cs ColorTransferMode.cs RgbColor.cs HsvColor.cs FastBitmapExtensions.cs Utility.cs PhotoEffect.cs

all: Apachai.Effects.dll

Apachai.Effects.dll: $(FILES)
	dmcs /t:library /r:System.Drawing.dll /out:Apachai.Effects.dll /unsafe $(FILES)

clean:
	rm -f Apachai.Effects.dll