using System;
using GXPEngine.Core;

namespace GXPEngine.Web
{
	/// <summary>
	/// SoundSystem backed by the Web Audio API (js/audio.js). Sounds are fetched and
	/// decoded in the background; playing one that is still loading starts it as
	/// soon as it is ready. Browsers keep audio muted until the first key press or
	/// click on the page.
	/// </summary>
	public class WebSoundSystem : SoundSystem
	{
		public override void Init() { }
		public override void Deinit() { }
		public override void Step() { }

		public override IntPtr CreateStream(string filename, bool looping) {
			return (IntPtr)Host.SoundLoad(AssetPath.Url(filename), looping, true);
		}

		public override IntPtr LoadSound(string filename, bool looping) {
			return (IntPtr)Host.SoundLoad(AssetPath.Url(filename), looping, false);
		}

		public override uint PlaySound(IntPtr id, uint channelId, bool paused) {
			return PlaySound(id, channelId, paused, 1, 0);
		}

		public override uint PlaySound(IntPtr id, uint channelId, bool paused, float volume, float pan) {
			return Host.SoundPlay((uint)id, paused ? 1 : 0, volume, pan);
		}

		public override float GetChannelFrequency(uint channelId) { return Host.ChannelGet(channelId, (int)ChannelProperty.Frequency); }
		public override void SetChannelFrequency(uint channelId, float frequency) { Host.ChannelSet(channelId, (int)ChannelProperty.Frequency, frequency); }
		public override float GetChannelPan(uint channelId) { return Host.ChannelGet(channelId, (int)ChannelProperty.Pan); }
		public override void SetChannelPan(uint channelId, float pan) { Host.ChannelSet(channelId, (int)ChannelProperty.Pan, pan); }
		public override float GetChannelVolume(uint channelId) { return Host.ChannelGet(channelId, (int)ChannelProperty.Volume); }
		public override void SetChannelVolume(uint channelId, float volume) { Host.ChannelSet(channelId, (int)ChannelProperty.Volume, volume); }
		public override bool GetChannelPaused(uint channelId) { return Host.ChannelGet(channelId, (int)ChannelProperty.Paused) != 0; }
		public override void SetChannelPaused(uint channelId, bool pause) { Host.ChannelSet(channelId, (int)ChannelProperty.Paused, pause ? 1 : 0); }
		public override bool ChannelIsPlaying(uint channelId) { return Host.ChannelGet(channelId, (int)ChannelProperty.Playing) != 0; }
		public override void StopChannel(uint channelId) { Host.ChannelStop(channelId); }
	}
}
