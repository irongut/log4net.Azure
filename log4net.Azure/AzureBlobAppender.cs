using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using log4net.Appender.Azure.Extensions;
using log4net.Appender.Azure.Language;
using log4net.Core;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace log4net.Appender.Azure
{
    public class AzureBlobAppender : BufferingAppenderSkeleton
    {
        private BlobServiceClient _account;
        private BlobContainerClient _cloudBlobContainer;

        public string ConnectionStringName { get; set; }
        private string _connectionString;

        public string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ConnectionStringName))
                {
                    return Util.GetConnectionString(ConnectionStringName);
                }
                if (String.IsNullOrWhiteSpace(_connectionString))
                    throw new ApplicationException(Resources.AzureConnectionStringNotSpecified);
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        private string _containerName;

        public string ContainerName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_containerName))
                    throw new ApplicationException(Resources.ContainerNameNotSpecified);
                return _containerName;
            }
            set
            {
                _containerName = value;
            }
        }

        private string _directoryName;

        public string DirectoryName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_directoryName))
                    throw new ApplicationException(Resources.DirectoryNameNotSpecified);
                return _directoryName;
            }
            set
            {
                _directoryName = value;
            }
        }

        /// <summary>
        /// Sends the events.
        /// </summary>
        /// <param name="events">The events that need to be send.</param>
        /// <remarks>
        /// <para>
        /// The subclass must override this method to process the buffered events.
        /// </para>
        /// </remarks>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            Parallel.ForEach(events, ProcessEvent);
        }

        private void ProcessEvent(LoggingEvent loggingEvent)
        {
            AppendBlobClient appendBlob = _cloudBlobContainer.GetAppendBlobClient(Filename(loggingEvent, _directoryName));
            var xml = loggingEvent.GetXmlString(Layout);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                appendBlob.AppendBlock(ms);
            }
        }

        private static string Filename(LoggingEvent loggingEvent, string directoryName)
        {
            return string.Format("{0}/{1}.{2}.entry.log.xml",
                                 directoryName,
                                 loggingEvent.TimeStamp.ToString("yyyy_MM_dd_HH_mm_ss_fffffff", DateTimeFormatInfo.InvariantInfo),
                                 Guid.NewGuid().ToString().ToLower());
        }

        /// <summary>
        /// Initialize the appender based on the options set
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is part of the <see cref="T:log4net.Core.IOptionHandler"/> delayed object activation scheme.
        /// The <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> method must be called on this object after the configuration properties have been set.
        /// Until <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> is called this object is in an undefined state and must not be used.
        /// </para>
        /// <para>
        /// If any of the configuration properties are modified then <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> must be called again.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();

            _account = new BlobServiceClient(ConnectionString);
            _cloudBlobContainer = _account.GetBlobContainerClient(ContainerName.ToLower());
            _cloudBlobContainer.CreateIfNotExists();
        }
    }
}
