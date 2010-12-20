
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

all: apachai.dll

apachai.dll: $(FILES) $(APACHAI_EFFECTS)
	dmcs /pkg:taglib-sharp /r:System.Web.dll /r:ServiceStack.Redis.dll /pkg:manos /r:$(APACHAI_EFFECTS) /debug /out:apachai.dll /t:library $(FILES)

clean:
	rm -f apachai.dll apachai.dll.mdb