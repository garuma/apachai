
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
	ConfigManager.cs

APACHAI_EFFECTS = Effects/Apachai.Effects.dll
APACHAI_EFFECTS_DIR = Effects/

all: apachai.dll Apachai.Effects.dll

apachai.dll: $(FILES) $(APACHAI_EFFECTS)
	dmcs /pkg:taglib-sharp /r:System.Web.dll /r:ServiceStack.Redis.dll /pkg:manos /r:$(APACHAI_EFFECTS) /debug /out:apachai.dll /t:library $(FILES)

Apachai.Effects.dll:
	make -C $(APACHAI_EFFECTS_DIR) && ln -s $(APACHAI_EFFECTS) .

clean:
	rm -f apachai.dll apachai.dll.mdb