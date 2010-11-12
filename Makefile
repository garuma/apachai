all: apachai.dll

apachai.dll: Apachai.cs
	dmcs /pkg:taglib-sharp /r:System.Web.dll /r:/usr/local/lib/manos/Manos.dll /debug /out:apachai.dll /t:library *.cs

clean:
	rm -f apachai.dll apachai.dll.mdb