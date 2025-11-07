using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace SphinxQueryCore.Net.Controllers
{
	public class HomeController : Controller
	{
		public async Task<IActionResult> Index(string host, int? port, string statement)
		{
			var model = await LoadModel(new IndexModel
			{
				Host = host,
				Port = port,
				Statement = statement,
			});

			return View(model);
		}

		private async Task<IndexModel> LoadModel(IndexModel prms)
		{
			if (prms == null || string.IsNullOrEmpty(prms.Host)) return new IndexModel();

			MySqlConnection conn = null;

			try
			{
				var connStrBuilder = new MySqlConnectionStringBuilder();
				connStrBuilder.Server = prms.Host;
				connStrBuilder.Port = (uint) prms.Port;
				connStrBuilder.CharacterSet = "utf8";
				connStrBuilder.Pooling = false;
				connStrBuilder.SslMode = MySqlSslMode.Disabled;

				conn = new MySqlConnection(connStrBuilder.ToString());

				try
				{
					await conn.OpenAsync();
				}
				catch
				{
					// workaround http://sphinxsearch.com/bugs/view.php?id=2196
					// for version of Sphinx earlier than 2.2.9
					if (conn.State != System.Data.ConnectionState.Open)
					{
						throw;
					}
				}

				var m = new IndexModel
				{
					Indexes = await MakeQueryAsync("show tables", conn),
				};

				if (!string.IsNullOrEmpty(prms.Statement))
				{
					m.Rows = MakeQueryAsync(prms.Statement, conn).Result;
					m.Meta = MakeQueryAsync("show meta", conn).Result;
					m.Status = MakeQueryAsync("show status", conn).Result;
				}

				return m;
			}
			catch (Exception x)
			{
				return new IndexModel { Exception = x };
			}
			finally
			{
				conn?.Close();
			}
		}

		private async Task<QueryResult> MakeQueryAsync(string statement, MySqlConnection conn)
		{
			var cmd = new MySqlCommand(statement, conn);
			using (var r = await cmd.ExecuteReaderAsync())
			{
				return new QueryResult
				{
					ColumnNames = Enumerable.Repeat("0", r.FieldCount)
						.Select((a, i) => r.GetName(i))
						.ToArray(),
					Values = ReadAllValues(r)
						.ToArray(),
				};
			}
		}

		private IEnumerable<object[]> ReadAllValues(DbDataReader r)
		{
			var row = new List<object>();

			while (r.Read())
			{
				row.Clear();

				for (int i = 0; i < r.FieldCount; i++)
				{
					row.Add(r.GetValue(i));
				}

				yield return row.ToArray();
			}
		}
	}
}
