using R3;
using System;
using System.IO;
using TextToTalk.Lexicons;

namespace TextToTalk.Backends.System
{
    public class SystemSoundQueue : SoundQueue<SystemSoundQueueItem>
    {
        internal ISpeechSynthesizer speechSynthesizer;
        private readonly Func<ISpeechSynthesizer>? speechSynthesizerFactory;
        private readonly LexiconManager lexiconManager;
        private readonly StreamSoundQueue streamSoundQueue;
        internal int consecutiveFailures;

        public Observable<SelectVoiceFailedException> SelectVoiceFailed => selectVoiceFailed;
        private readonly Subject<SelectVoiceFailedException> selectVoiceFailed;
        public SystemSoundQueue(LexiconManager lexiconManager, PluginConfiguration config, ISpeechSynthesizer speechSynthesizer, Func<ISpeechSynthesizer>? synthesizerFactory = null)
            : this(lexiconManager, config, speechSynthesizer, new StreamSoundQueue(config), synthesizerFactory)
        {
        }

        internal SystemSoundQueue(LexiconManager lexiconManager, PluginConfiguration config, ISpeechSynthesizer speechSynthesizer, StreamSoundQueue streamSoundQueue, Func<ISpeechSynthesizer>? synthesizerFactory = null)
        {
            this.streamSoundQueue = streamSoundQueue;
            this.lexiconManager = lexiconManager;
            this.speechSynthesizer = speechSynthesizer;
            this.speechSynthesizerFactory = synthesizerFactory;
            this.selectVoiceFailed = new Subject<SelectVoiceFailedException>();
        }

        public void EnqueueSound(VoicePreset preset, TextSource source, string text)
        {
            AddQueueItem(new SystemSoundQueueItem
            {
                Preset = preset,
                Text = text,
                Source = source,
            });
        }

        protected override void OnSoundLoop(SystemSoundQueueItem nextItem)
        {
            if (nextItem.Preset is not SystemVoicePreset systemVoicePreset)
            {
                throw new InvalidOperationException("Invalid voice preset provided.");
            }

            try
            {
                this.speechSynthesizer.UseVoicePreset(nextItem.Preset);
            }
            catch (SelectVoiceFailedException e)
            {
                DetailedLog.Error(e, "Failed to select voice {0}", systemVoicePreset.VoiceName ?? "");
                this.selectVoiceFailed.OnNext(e);
            }

            var ssml = this.lexiconManager.MakeSsml(nextItem.Text,
                langCode: this.speechSynthesizer.VoiceCultureIetfLanguageTag);
            DetailedLog.Verbose(ssml);

            MemoryStream? synthesisStream = null;
            var synthesisSucceeded = false;
            try
            {
                synthesisStream = new MemoryStream();
                this.speechSynthesizer.SetOutputToWaveStream(synthesisStream);
                this.speechSynthesizer.SpeakSsml(ssml);

                synthesisSucceeded = true;
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                DetailedLog.Error(e, "TTS playback failed: {0}", ssml);
                TryRecoverSynthesizer();
            }
            if (synthesisSucceeded && synthesisStream != null)
            {
                this.consecutiveFailures = 0;
                synthesisStream.Seek(0, SeekOrigin.Begin);
                this.streamSoundQueue.EnqueueSound(synthesisStream, nextItem.Source, StreamFormat.Wave, 1f);
            }
            else
            {
                synthesisStream?.Dispose();
            }
        }

        public override void CancelAllSounds()
        {
            base.CancelAllSounds();
            this.streamSoundQueue.CancelAllSounds();
        }

        public override void CancelFromSource(TextSource source)
        {
            base.CancelFromSource(source);
            this.streamSoundQueue.CancelFromSource(source);
        }


        protected override void OnSoundCancelled()
        {
            try
            {
                this.speechSynthesizer.SetOutputToNull();
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }
            catch (Exception e)
            {
                DetailedLog.Error(e, "Failed to set speech synthesizer output to null during cancellation.");
            }
        }

        private void TryRecoverSynthesizer()
        {
            this.consecutiveFailures++;
            if (this.consecutiveFailures >= 3 && this.speechSynthesizerFactory != null)
            {
                DetailedLog.Warn(
                    $"Speech synthesizer has failed {this.consecutiveFailures} times consecutively; recreating.");
                try
                {
                    var oldSynth = this.speechSynthesizer;
                    this.speechSynthesizer = this.speechSynthesizerFactory();
                    oldSynth.Dispose();
                    this.consecutiveFailures = 0;
                }
                catch (ObjectDisposedException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    DetailedLog.Error(ex, "Failed to recreate speech synthesizer.");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.speechSynthesizer.Dispose();
                this.streamSoundQueue.Dispose();
            }
        }
    }
}
