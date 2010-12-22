
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Apachai
{
	public class ConfigManager
	{
		public class ConfigException : Exception
		{
			public ConfigException (string message) : base (message)
			{
			}

			internal static void ThrowKeyNotFound (string key)
			{
				throw new ConfigException (string.Format ("Key ({0}) wasn't found in config file", key));
			}

			internal static void ThrowKeyMalformed (string key)
			{
				throw new ConfigException (string.Format ("Key ({0}) isn't of correct type or malformed", key));
			}
		}

		string filepath;
		Dictionary<object, object> store;

		public ConfigManager (string filepath)
		{
			this.filepath = filepath;
			ProcessConfigFile ();
		}

		void ProcessConfigFile ()
		{
			string[] lines = File.ReadAllLines (filepath)
				.Where (l => { var t = l.TrimStart (null); return t.Length < 2 || t[0] != '/' || t[1] != '/'; }).ToArray ();

			store = JSON.JsonDecode (lines.Aggregate (string.Concat)) as Dictionary<object, object>;

            if (store == null)
               throw new ConfigException ("Config file is malformed JSON. How Fascinating!");
		}

		public bool Get<T> (string key, out T value)
		{
			value = default (T);
			var tmp = store[key];
			if (!(tmp is T))
				return false;

			value = (T)tmp;
			Console.WriteLine ("Config manager: {0} => {1}", key, value.ToString ());

			return true;
		}

		public T GetOrDefault<T> (string key, T defaultValue)
		{
			T tmp;
			try {
				return Get (key, out tmp) ? tmp : defaultValue;
			} catch {
				return defaultValue;
			}
		}

		public T GetOrThrow<T> (string key)
		{
			T tmp;
			if (!Get<T> (key, out tmp))
				ConfigException.ThrowKeyMalformed (key);

			return tmp;
		}
	}
}