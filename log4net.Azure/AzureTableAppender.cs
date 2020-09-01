using log4net.Appender.Azure.Extensions;
using log4net.Appender.Azure.Language;
using log4net.Core;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;

namespace log4net.Appender.Azure
{
	public class AzureTableAppender : BufferingAppenderSkeleton
	{
		private CloudStorageAccount _account;
		private CloudTableClient _client;

		public string ConnectionStringName { get; set; }

		private string _connectionString;
		public string ConnectionString
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(ConnectionStringName))
				{
					return Util.GetConnectionString(ConnectionStringName);
				}
				if (String.IsNullOrEmpty(_connectionString))
					throw new ApplicationException(Resources.AzureConnectionStringNotSpecified);
				return _connectionString;
			}
			set
			{
				_connectionString = value;
			}
		}

		private string _tableName;
		public string TableName
		{
			get
			{
				if (String.IsNullOrEmpty(_tableName))
					throw new ApplicationException(Resources.TableNameNotSpecified);
				return _tableName;
			}
			set
			{
				_tableName = value;
			}
		}

		protected CloudTable Table { get; private set; }

		public bool PropAsColumn { get; set; }

		public PartitionKeyTypeEnum PartitionKeyType { get; set; } = PartitionKeyTypeEnum.LoggerName;

		protected override void SendBuffer(LoggingEvent[] events)
		{
			var grouped = events.Select(GetLogEntity).GroupBy(evt => evt.PartitionKey);

			foreach (var group in grouped)
			{
				foreach (var batch in group.Batch(100))
				{
					var batchOperation = new TableBatchOperation();
					foreach (var azureLoggingEvent in batch)
					{
						batchOperation.Insert(azureLoggingEvent);
					}
					Table.ExecuteBatch(batchOperation);
				}
			}
		}

		protected ITableEntity GetLogEntity(LoggingEvent @event)
		{
			if (Layout != null)
			{
				return new AzureLayoutLoggingEventEntity(@event, PartitionKeyType, Layout);
			}

			return PropAsColumn
				? (ITableEntity)new AzureDynamicLoggingEventEntity(@event, PartitionKeyType)
				: new AzureLoggingEventEntity(@event, PartitionKeyType);
		}

		public override void ActivateOptions()
		{
			base.ActivateOptions();

			_account = CloudStorageAccount.Parse(ConnectionString);
			_client = _account.CreateCloudTableClient();
			Table = _client.GetTableReference(TableName);
			Table.CreateIfNotExists();
		}
	}
}