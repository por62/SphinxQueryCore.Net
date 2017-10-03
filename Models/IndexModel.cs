using System;

namespace SphinxQueryCore.Net
{
	public class IndexModel
	{
		public string Host;
		public int? Port;
		public string Statement;

		public Exception Exception;

		public QueryResult Indexes;
		public QueryResult Rows;
		public QueryResult Meta;
		public QueryResult Status;
	}

	public class QueryResult
	{
		public string[] ColumnNames;
		public object[][] Values;
	}
}
