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
using System.Text;

using ServiceStack.Redis;

namespace Apachai
{
	public class BackingStore
	{
		static IRedisClientsManager redisManager = new BasicRedisClientManager ();

		/* Possible keys with that prefix
		 */
		const string picPrefix = "apachai:pictures:";
		const string picInfos = picPrefix + "infos:";
		const string picTweet = picPrefix + "tweet:";
		const string picUser = picPrefix + "user:";
		const string picShortUrl = picPrefix + "shortUrl:";

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

		public string GetOrSetPictureInfos (string filename, Func<string> dataCreator)
		{
			var redis = redisManager.GetClient ();
			string key = picInfos + filename;
			if (redis.ContainsKey (key))
				return redis[key];

			return redis[key] = dataCreator ();
		}

		public string GetShortUrlForImg (string image)
		{
			var redis = redisManager.GetClient ();
			return redis[picShortUrl + image];
		}

		public bool DoWeKnowUser (long id)
		{
			var redis = redisManager.GetClient ();
			return redis.SetContainsItem (idList, id.ToString ());
		}

		public void RegisterImageWithTweet (long uid, string picture, string tweet, string shortUrl)
		{
			var redis = redisManager.GetClient ();

			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			string id = uid.ToString ();
			redis.AddItemToList (userPictures + id, picture);
			redis[picTweet + picture] = tweet;
			redis[picUser + picture] = id;
			redis[picShortUrl + picture] = shortUrl;
		}

		public void GetTwitterInfosFromImage (string pictureId, out string avatarUrl, out string tweetText)
		{
			var redis = redisManager.GetClient ();

			avatarUrl = tweetText = string.Empty;
			if (!redis.ContainsKey (picUser + pictureId))
				return;

			avatarUrl = redis[userAvatarUrl + redis[picUser + pictureId]];
			tweetText = redis[picTweet + pictureId];
		}

		public void SetUserInfos (long uid, string screenName)
		{
			var redis = redisManager.GetClient ();

			string id = uid.ToString ();
			redis[userScreenName + id] = screenName;
			redis.AddItemToSet (idList, id);
		}

		public void SetExtraUserInfos (long uid, string avatarUrl, string realName)
		{
			var redis = redisManager.GetClient ();

			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			string id = uid.ToString ();
			redis[userRealName + id] = realName;
			redis[userAvatarUrl + id] = avatarUrl;
		}

		public bool GetExtraUserInfos (long uid, out string avatarUrl, out string realName)
		{
			var redis = redisManager.GetClient ();

			avatarUrl = realName = string.Empty;

			if (!DoWeKnowUser (uid))
				return false;

			string id = uid.ToString ();
			avatarUrl = redis[userAvatarUrl + id];
			realName = redis[userRealName + id];

			return true;
		}

		public void SetUserAccessTokens (long uid, string accessToken, string accessTokenSecret)
		{
			var redis = redisManager.GetClient ();

			string id = uid.ToString ();
			redis[userAccessToken + id] = accessToken;
			redis[userAccessTokenSecret + id] = accessTokenSecret;
		}

		public OAuthToken GetUserAccessTokens (long uid)
		{
			var redis = redisManager.GetClient ();

			if (!DoWeKnowUser (uid))
				throw new ArgumentException ("User is unknown is the database");

			string id = uid.ToString ();

			return new OAuthToken (redis[userAccessToken + id], redis[userAccessTokenSecret + id]);
		}

		public void SaveTempTokenSecret (string token, string tokenSecret)
		{
			var redis = redisManager.GetClient ();

			redis["apachai:tokenSecrets:" + token] = tokenSecret;
		}

		public string GetTempTokenSecret (string token)
		{
			var redis = redisManager.GetClient ();

			var result = redis["apachai:tokenSecrets:" + token];
			redis.Remove ("apachai:tokenSecrets:" + token);

			return result;
		}
	}
}