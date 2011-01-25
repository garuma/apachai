
FILES = Apachai.cs \
	Hasher.cs \
	JsonStringDictionary.cs \
	redis-sharp.cs \
	StaticContentModule.cs \
	OAuth.cs \
	BackingStore.cs \
	TagLibMetadata.cs \
	Json.cs \
	Twitter.cs \
	UrlShortener.cs \
	AccessLogger.cs \
	ConfigManager.cs \
	PictureContentModule.cs \
	HtmlPaths.cs

APACHAI_EFFECTS = Effects/Apachai.Effects.dll
APACHAI_EFFECTS_DIR = Effects/

all: apachai.dll

apachai.dll: $(FILES) Apachai.Effects.dll
	dmcs /pkg:taglib-sharp /r:System.Web.dll /r:ServiceStack.Redis.dll /pkg:manos /r:$(APACHAI_EFFECTS) /r:System.Drawing.dll /debug /out:apachai.dll /t:library $(FILES)

Apachai.Effects.dll: $(APACHAI_EFFECTS)
	make -C $(APACHAI_EFFECTS_DIR) && ln -s $(APACHAI_EFFECTS) .

clean:
	rm -f apachai.dll apachai.dll.mdb