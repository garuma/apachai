
using System;
using System.Text;

namespace Apachai
{
	public class JsonStringDictionary
	{
		StringBuilder sb = new StringBuilder ();
		bool finished;

		public JsonStringDictionary ()
		{
			sb.Append ('{');
		}

		public string Json {
			get {
				if (!finished) {
					sb[sb.Length - 1] = '}';
					finished = true;
				}
				
				return sb.ToString ();
			}
		}

		public string this[string key] {
			set {
				if (finished)
					return;

				sb.Append ('"');
				sb.Append (key);
				sb.Append ('"');
				sb.Append (':');
				sb.Append ('"');
				sb.Append (value);
				sb.Append ('"');
				sb.Append (',');
			}
		}
	}
}