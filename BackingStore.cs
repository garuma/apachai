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
		const string picInfosCache = picPrefix + "cache:";
		const string picLinksCache = picPrefix + "links:cache";

		/* Possible keys with that prefix (the twitterId is stored in some cookies) :
		     userPrefix + "infos:realname:" + {twitterId} -> the user real name
		     userPrefix + "infos:avatarUrl:" + {twitterId} -> the URL to the avatar pic
		     userPrefix + "infos:accessToken:" + {twitterId} -> get the OAuth access token
		     userPrefix + "infos:accessTokenSecret:" + {twitterId} -> get the OAuth access token secret
		     userPrefix + "pictures:" + {twitterId} -> list of photos id for the user
		     userPrefix + "infos:screenName" + {twitterId} -> screen name of the user (nickname)
		     userPrefix + "infos:desc" + {twitterId} -> description of user on his profile
		     userPrefix + "infos:stale" + {twitterId} -> an expirable key that says if we should refresh infos fields
		*/
		const string userPrefix = "apachai:users:";
		const string userScreenName = userPrefix + "infos:screenName:";
		const string userAvatarUrl = userPrefix + "infos:avatarUrl:";
		const string userRealName = userPrefix + "infos:realName:";
		const string userUrl = userPrefix + "infos:url:";
		const string userDesc = userPrefix + "infos:desc:";
		const string userStale = userPrefix + "infos:stale";
		const string userAccessToken = userPrefix + "infos:accessToken:";
		const string userAccessTokenSecret = userPrefix + "infos:accessTokenSecret:";
		const string userPictures = userPrefix + "pictures:";

		/* This is a set containing all the user ids we know about
		   If we want to test if we know an user, we just check if
		   the supplied id is in the set
		 */
		const string idList = "apachai:ids";

		/* The total number of picture uploaded (counter)
		 */
		const string pictureCount = "apachai:pictureCount";

		/* A buffer holding up to 10 recent elements
		 */
		const string pictureRecentBuffer = "apachai:pictureRecentBuffer";

		const string statsCache = "apachai:statsCache";

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

#region Picture informations
		public bool GetPicturesInfos (string filename, out string result)
		{
			return TryGetCachedInfos (picInfos, filename, out result);
		}

		public void SetPictureInfos (string filename, string data)
		{
			SetCachedInfos (picInfos, filename, data, TimeSpan.Zero);
		}

		public string GetShortUrlForImg (string image)
		{
			using (var redis = redisManager.GetClient ())
				return redis[picShortUrl + image];
		}

		public bool GetPictureGeo (string image, out string geo)
		{
			return TryGetCachedInfos (picGeo, image, out geo);
		}

		public void SetPictureGeo (string image, string geo)
		{
			SetCachedInfos (picGeo, image, geo, TimeSpan.Zero);
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
				redis.IncrementValue (pictureCount);
				redis.PrependItemToList (pictureRecentBuffer, picture);
				redis.TrimList (pictureRecentBuffer, 0, 10);
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

		public void GetTwitterInfosFromImage (string pictureId, out string avatarUrl, out string tweetText, out string screenname, out string name, out string url, out string desc)
		{
			avatarUrl = tweetText = screenname = name = url = desc = string.Empty;

			using (var redis = redisManager.GetClient ()) {
				if (!redis.ContainsKey (picUser + pictureId))
					return;

				tweetText = redis[picTweet + pictureId];
				string user = redis[picUser + pictureId];
				GetExtraUserInfos (user, out screenname, out avatarUrl, out name, out url, out desc);
			}
		}

		public void SetCachedTwitterInfos (string picture, string json)
		{
			SetCachedInfos (picInfosCache, picture, json, TimeSpan.FromDays (1));
		}


		public bool TryGetCachedTwitterInfos (string picture, out string json)
		{
			return TryGetCachedInfos (picInfosCache, picture, out json);
		}

		public void SetPictureLinks (string picture, string json)
		{
			SetCachedInfos (picLinksCache, picture, json, TimeSpan.Zero);
		}

		public bool TryGetPictureLinks (string picture, out string json)
		{
			return TryGetCachedInfos (picLinksCache, picture, out json);
		}
#endregion

#region User informations
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

		public bool DoesUserNeedInfoUpdate (long id)
		{
			if (!DoWeKnowUser (id))
				return true;

			using (var redis = redisManager.GetClient ())
				return redis.GetTimeToLive (userStale + id.ToString ()) < TimeSpan.Zero;
		}

		public void SetUserInfos (long uid, string screenName)
		{
			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis[userScreenName + id] = screenName;
				redis.AddItemToSet (idList, id);
			}
		}

		public void SetExtraUserInfos (long uid, string avatarUrl, string realName, string url, string desc)
		{
			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			using (var redis = redisManager.GetClient ()) {
				string id = uid.ToString ();
				redis[userRealName + id] = realName;
				redis[userAvatarUrl + id] = avatarUrl;
				redis[userUrl + id] = url;
				redis[userDesc + id] = desc;
				redis.SetEntry (userStale + id.ToString (), "foo", TimeSpan.FromDays (7));
			}
		}

		public bool GetExtraUserInfos (string id,
		                               out string screenname,
		                               out string avatarUrl,
		                               out string realName,
		                               out string url,
		                               out string desc)
		{
			avatarUrl = screenname = realName = url = desc = string.Empty;

			if (!DoWeKnowUser (long.Parse (id)))
				return false;

			using (var redis = redisManager.GetClient ()) {
				avatarUrl = redis[userAvatarUrl + id];
				realName = redis[userRealName + id];
				url = redis[userUrl + id];
				desc = redis[userDesc + id];
				screenname = redis[userScreenName + id];
			}

			return true;
		}
#endregion

#region Twitter OAuth tokens management
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
#endregion

#region /s/
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
#endregion

#region /stats
		public void GetCountStats (out int picCount, out int userCount)
		{
			picCount = userCount = 0;

			using (var redis = redisManager.GetClient ()) {
				picCount = int.Parse (redis[pictureCount]);
				userCount = redis.GetSetCount (idList);
			}
		}

		public List<string> GetLastPicturesIds ()
		{
			using (var redis = redisManager.GetClient ()) {
				return redis.GetAllItemsFromList (pictureRecentBuffer);
			}
		}

		public void SetCachedStats (string json)
		{
			SetCachedInfos (statsCache, string.Empty, json, TimeSpan.FromSeconds (50));
		}

		public bool TryGetCachedStats (out string json)
		{
			return TryGetCachedInfos (statsCache, string.Empty, out json);
		}
#endregion

#region Helpers
		void SetCachedInfos (string ns, string id, string json, TimeSpan expire)
		{
			using (var redis = redisManager.GetClient ()) {
				string key = ns + id;
				if (expire == TimeSpan.Zero)
					redis.SetEntry (key, json);
				else
					redis.SetEntry (key, json, expire);
			}
		}

		bool TryGetCachedInfos (string ns, string id, out string json)
		{
			json = string.Empty;
			using (var redis = redisManager.GetClient ()) {
				string key = ns + id;
				return !string.IsNullOrEmpty (json = redis.GetValue (key));
			}
		}
#endregion
	}
}