using System.Threading.Channels;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// Signalling channel for pushing changes to the synchronisation server
	/// </summary>
	/// <remarks>
	/// When a byte (any byte) is written to the writer, that will be picked up by the reader in
	/// <see cref="SyncMonitor.MonitorAsync"/>, which manages the flow of data.
	/// </remarks>
	public sealed class SyncChannel
	{
		private Channel<byte> ExportQueue { get; } = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
		{
			FullMode = BoundedChannelFullMode.DropWrite,
			SingleReader = true
		});

		public ChannelReader<byte> Reader => this.ExportQueue.Reader;
		public ChannelWriter<byte> Writer => this.ExportQueue.Writer;

		public ValueTask TouchAsync() => this.Writer.WriteAsync(0x00);
	}
}
