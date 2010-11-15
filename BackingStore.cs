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

namespace Apachai
{
	public class BackingStore
	{
		static Redis redis = new Redis ();

		/* Possible keys with that prefix
		 */
		const string picPrefix = "apachai:pictures:";
		const string picInfos = picPrefix + "infos:";
		const string picTweet = picPrefix + "tweet:";

		/* Possible keys with that prefix (the twitterPrefix is stored in some cookies) :
		     userPrefix + "infos:realname:" + {twitterId} -> the user real name
		     userPrefix + "infos:avatarUrl:" + {twitterId} -> the URL to the avatar pic
		     userPrefix + "infos:accessToken:" + {twitterId} -> get the OAuth access token
		     userPrefix + "pictures:" + {twitterId} -> list of photos id for the user
		*/
		const string userPrefix = "apachai:users:";
		const string userRealName = userPrefix + "infos:realname:";
		const string userAvatarUrl = userPrefix + "infos:avatarUrl:";
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
			string key = picInfos + filename;
			if (redis.ContainsKey (key))
				return redis[key];

			return redis[key] = dataCreator ();
		}

		public bool DoWeKnowUser (long id)
		{
			return redis.IsMemberOfSet (idList, Encoding.UTF8.GetBytes (id.ToString ()));
		}

		public void SetUserInfos (long uid, string realName, string avatarUrl)
		{
			string id = uid.ToString ();
			redis[userRealName + id] = realName;
			redis[userAvatarUrl + id] = avatarUrl;
			redis.AddToSet (idList, Encoding.UTF8.GetBytes (id)); 
		}

		public void SetUserAccessTokens (long uid, string accessToken, string accessTokenSecret)
		{
			string id = uid.ToString ();
			redis[userAccessToken + id] = accessToken;
			redis[userAccessTokenSecret + id] = accessTokenSecret;
		}

		public void SaveTempTokenSecret (string token, string tokenSecret)
		{
			redis["apachai:tokenSecrets:" + token] = tokenSecret;
		}

		public string GetTempTokenSecret (string token)
		{
			var result = redis["apachai:tokenSecrets:" + token];
			redis.Remove ("apachai:tokenSecrets:" + token);

			return result;
		}
	}
}