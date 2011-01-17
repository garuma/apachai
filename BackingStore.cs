//
// Copyright (c) 2010 Jérémie "garuma" Laval
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using ServiceStack.Redis;

namespace Apachai
{
	public class BackingStore
	{
		class BackingStoreException : ApplicationException
		{
			public BackingStoreException (string message) : base (message)
			{
			}

			public BackingStoreException (Exception inner) : base ("Error while initializing backing store", inner)
			{
			}
		}

		/* Possible keys with that prefix
		 */
		const string picPrefix = "apachai:pictures:";
		const string picInfos = picPrefix + "infos:";
		const string picTweet = picPrefix + "tweet:";
		const string picUser = picPrefix + "user:";
		const string picLongUrl = picPrefix + "longUrl:";
		const string picShortUrl = picPrefix + "shortUrl:";
		const string picGeo = picPrefix + "geo:";

		/* Possible keys with that prefix (the twitterId is stored in some cookies) :
		     userPrefix + "infos:realname:" + {twitterId} -> the user real name
		     userPrefix + "infos:avatarUrl:" + {twitterId} -> the URL to the avatar pic
		     userPrefix + "infos:accessToken:" + {twitterId} -> get the OAuth access token
		     userPrefix + "pictures:" + {twitterId} -> list of photos id for the user
		*/
		const string userPrefix = "apachai:users:";
		const string userScreenName = userPrefix + "infos:screenName:";
		const string userAvatarUrl = userPrefix + "infos:avatarUrl:";
		const string userRealName = userPrefix + "infos:realName:";
		const string userAccessToken = userPrefix + "infos:accessToken:";
		const string userAccessTokenSecret = userPrefix + "infos:accessTokenSecret:";
		const string userPictures = userPrefix + "pictures:";

		/* This is a set containing all the user ids we know about
		   If we want to test if we know an user, we just check if
		   the supplied id is in the set
		 */
		const string idList = "apachai:ids";

		/* This contain a counter that is incremented at each picture uploaded and allow to point to
		 * it in a shorter but possibly volatile way
		 */
		const string picShortId = picPrefix + "shortIdCounter";
		// This is the prefix by which we map a short id to its permalink counter part
		const string picShortIdMap = picPrefix + "shortIdMap:";

		IRedisClientsManager redisManager;

		public BackingStore (ConfigManager cfg)
		{
			try {
				var raw = cfg.GetOrDefault<IList<object>> ("redisServers", new [] { "127.0.0.1" });
				redisManager = new BasicRedisClientManager (raw.Cast<string> ().ToArray ());
			} catch (Exception e) {
				throw new BackingStoreException (e);
			}
		}

		public bool GetPicturesInfos (string filename, out string result)
		{
			result = string.Empty;
			using (var redis = redisManager.GetClient ()) {
				string key = picInfos + filename;
				if (redis.ContainsKey (key)) {
					result = redis[key];
					return true;
				}

				return false;
			}
		}

		public void SetPictureInfos (string filename, string data)
		{
			using (var redis = redisManager.GetClient ())
				redis [picInfos + filename] = data;
		}

		public string GetOrSetPictureInfos (string filename, Func<string> dataCreator)
		{
			using (var redis = redisManager.GetClient ())
				return redis[picInfos + filename] = dataCreator ();
		}

		public string GetShortUrlForImg (string image)
		{
			using (var redis = redisManager.GetClient ())
				return redis[picShortUrl + image];
		}

		public bool GetPictureGeo (string image, out string geo)
		{
			geo = string.Empty;

			using (var redis = redisManager.GetClient ()) {
				string key = picGeo + image;
				if (!redis.ContainsKey (key))
					return false;

				geo = redis[key];
				return true;
			}
		}

		public void SetPictureGeo (string image, string geo)
		{
			using (var redis = redisManager.GetClient ())
				redis[picGeo + image] = geo;
		}

		public bool DoWeKnowUser (long id)
		{
			using (var redis = redisManager.GetClient ())
				return redis.SetContainsItem (idList, id.ToString ());
		}

		public bool DoWeKnowUser (long id, string token)
		{
			if (!DoWeKnowUser (id))
				return false;

			using (var redis = redisManager.GetClient ())
				return redis[userAccessToken + id.ToString ()].Equals (token, StringComparison.Ordinal);
		}

		public void RegisterImageWithTweet (long uid, string picture, string tweet, string longUrl, string shortUrl)
		{
			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis.PrependItemToList (userPictures + id, picture);
				redis[picTweet + picture] = tweet;
				redis[picUser + picture] = id;
				redis[picLongUrl + picture] = longUrl;
				redis[picShortUrl + picture] = shortUrl;
			}
		}

		public List<string> GetImagesOfUserFromPic (string picture, int count)
		{
			using (var redis = redisManager.GetClient ()) {
				string id = redis[picUser + picture];
				return redis.GetRangeFromList (userPictures + id, 0, count);
			}
		}

		public void MapShortToLongUrl (string shortId, string longId)
		{
			using (var redis = redisManager.GetClient ())
				redis[picShortIdMap + shortId] = longId;
		}

		public void GetTwitterInfosFromImage (string pictureId, out string avatarUrl, out string tweetText, out string name)
		{
			avatarUrl = tweetText = name = string.Empty;

			using (var redis = redisManager.GetClient ()) {
				if (!redis.ContainsKey (picUser + pictureId))
					return;
				avatarUrl = redis[userAvatarUrl + redis[picUser + pictureId]];
				name = redis[userScreenName + redis[picUser + pictureId]];
				tweetText = redis[picTweet + pictureId];
			}
		}

		public void SetUserInfos (long uid, string screenName)
		{
			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis[userScreenName + id] = screenName;
				redis.AddItemToSet (idList, id);
			}
		}

		public void SetExtraUserInfos (long uid, string avatarUrl, string realName)
		{
			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis[userRealName + id] = realName;
				redis[userAvatarUrl + id] = avatarUrl;
			}
		}

		public bool GetExtraUserInfos (long uid, out string avatarUrl, out string realName)
		{
			avatarUrl = realName = string.Empty;

			if (!DoWeKnowUser (uid))
				return false;

			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				avatarUrl = redis[userAvatarUrl + id];
				realName = redis[userRealName + id];
			}

			return true;
		}

		public void SetUserAccessTokens (long uid, string accessToken, string accessTokenSecret)
		{
			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis[userAccessToken + id] = accessToken;
				redis[userAccessTokenSecret + id] = accessTokenSecret;
			}
		}

		public OAuthToken GetUserAccessTokens (long uid)
		{
			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();

				return new OAuthToken (redis[userAccessToken + id], redis[userAccessTokenSecret + id]);
			}
		}

		public void SaveTempTokenSecret (string token, string tokenSecret)
		{
			using (var redis = redisManager.GetClient ())
				redis["apachai:tokenSecrets:" + token] = tokenSecret;
		}

		public string GetTempTokenSecret (string token)
		{
			using (var redis = redisManager.GetClient ()) {
				var result = redis["apachai:tokenSecrets:" + token];
				redis.Remove ("apachai:tokenSecrets:" + token);

				return result;
			}
		}

		public long GetNextShortId ()
		{
			using (var redis = redisManager.GetClient ())
				return redis.IncrementValue (picShortId);
		}

		public bool FindPermaFromShort (string shortId, out string permaId)
		{
			permaId = string.Empty;

			using (var redis = redisManager.GetClient ()) {
				if (!redis.ContainsKey (picShortIdMap + shortId))
					return false;

				permaId = redis[picShortIdMap + shortId];
				return true;
			}
		}
	}
}