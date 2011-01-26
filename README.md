# Apachaï
## Picture uploading for the rest of us

Apachaï is designed to be a small and lightweight photo and picture sharing application (for services like Twitter) built on the [Manos framework](https://github.com/jacksonh/manos). A running instance is available at [apch.fr](http://apch.fr/).

## Dependencies

It depends on a [Redis](http://redis.io/) server available and on [ServiceStack .NET binding](http://code.google.com/p/servicestack/wiki/ServiceStackRedis) to it (included).

Apachaï also leverages code from [TweetStation](https://github.com/migueldeicaza/TweetStation) and [Pinta](https://github.com/jpobst/Pinta) but it's not needed to have any of them installed.

Of course, you need Manos installed if you want to launch the application (no need for another webserver though).

## How to run

To run Apachaï you need to adjust a few things. First, rename `config.json.template` to `config.json` and adjust the settings there. Following is a small description for each of them:

+ `serverBaseUrl`: that's the URL of the host you deploying unto (example: [http://apch.fr](http://apch.fr/))
+ `twitterKey` and `twitterSecret`: get one over at http://dev.twitter.com
+ `twitterCallback`: theorically it should be set to something like `serverBaseUrl`/AuthCallback but who knows.
+ `testInstance`: a boolean, if `true` the application will work standalone and will never do a single call to Twitter. Useful for debugging purpose.
+ `imagesDirectory`: a path (relative or absolute) to a folder where your want the picture sent to be stored. Defaults to the 'Pictures' value (you have to create the folder in the root directory yourself though).
+ `redisServers`: an IP or host array of string where we can join Redis server instances, by default we simply use 127.0.0.1

Then just in case, walk a bit in the javascript code under Content/js as I may have hardcoded some stuff to http://apch.fr (promise, I try to avoid this but sometimes I'm lazy). A grep should do the trick.

Finally just hit `manos -s` in the root folder and you are all set.

## License

All code, markup and others are released under the permissive [MIT license](http://opensource.org/licenses/mit-license).

Copyright (c) Jérémie "garuma" Laval
