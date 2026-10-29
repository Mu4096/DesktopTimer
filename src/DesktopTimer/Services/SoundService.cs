using System.Diagnostics;
using System.IO;
using DesktopTimer.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DesktopTimer.Services;

/// <summary>
/// 音源のループ再生（仕様書 6.3）。同時に鳴らすのは1つだけで、新しく再生すると前の音は止まる。
/// </summary>
public sealed class SoundService : IDisposable
{
    private const string BuiltInBeepResourceName = "DesktopTimer.Assets.beep.wav";

    private WaveOut? _output;
    private WaveStream? _reader;
    private VolumeSampleProvider? _volume;

    public bool IsPlaying => _output is not null;

    /// <summary>
    /// 音源をループ再生する。音量 0 の場合は鳴らさない。
    /// ファイルが開けない場合は内蔵ビープで代替する。
    /// </summary>
    /// <param name="volumePercent">音量（0〜100）。</param>
    public void PlayLoop(SoundSource source, int volumePercent)
    {
        Stop();

        if (volumePercent <= 0)
        {
            return;
        }

        try
        {
            _reader = OpenReader(source);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"音源を開けないため内蔵ビープで代替します: {source.FilePath}: {ex.Message}");
            _reader = OpenReader(SoundCatalog.BuiltInBeep);
        }

        _volume = new VolumeSampleProvider(new LoopStream(_reader).ToSampleProvider());
        SetVolume(volumePercent);

        try
        {
            _output = new WaveOut();
            _output.Init(_volume);
            _output.Play();
        }
        catch (Exception ex)
        {
            // 出力デバイスがない等。アラームが鳴らないだけでアプリは続行する
            Debug.WriteLine($"音声を再生できません: {ex.Message}");
            Stop();
        }
    }

    /// <summary>音声ファイルとして開けるか（追加前の確認用）。</summary>
    public static bool CanOpen(string path)
    {
        try
        {
            using var reader = new AudioFileReader(path);
            return reader.TotalTime > TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"音声ファイルとして開けません: {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>再生中の音量を変える（試聴中のスライダー操作用）。</summary>
    /// <param name="volumePercent">音量（0〜100）。</param>
    public void SetVolume(int volumePercent)
    {
        if (_volume is not null)
        {
            _volume.Volume = Math.Clamp(volumePercent, 0, 100) / 100f;
        }
    }

    /// <summary>再生を止める。再生中でなければ何もしない。</summary>
    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
        _volume = null;
        _reader?.Dispose();
        _reader = null;
    }

    public void Dispose() => Stop();

    private static WaveStream OpenReader(SoundSource source)
    {
        if (source.FilePath is null)
        {
            // 内蔵ビープ。リソースのストリームはシークできない場合があるのでメモリに読み込む
            using var resource = typeof(SoundService).Assembly.GetManifestResourceStream(BuiltInBeepResourceName)
                ?? throw new InvalidOperationException($"埋め込みリソースがありません: {BuiltInBeepResourceName}");
            var memory = new MemoryStream();
            resource.CopyTo(memory);
            memory.Position = 0;
            return new WaveFileReader(memory);
        }

        // wav / mp3 どちらも開ける
        return new AudioFileReader(source.FilePath);
    }

    /// <summary>末尾まで読んだら先頭に戻して読み続けるストリーム。</summary>
    private sealed class LoopStream : WaveStream
    {
        private readonly WaveStream _source;

        public LoopStream(WaveStream source)
        {
            _source = source;
        }

        public override WaveFormat WaveFormat => _source.WaveFormat;

        public override long Length => _source.Length;

        public override long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var total = 0;
            while (total < count)
            {
                var read = _source.Read(buffer, offset + total, count - total);
                if (read == 0)
                {
                    if (_source.Position == 0)
                    {
                        // 空のファイル。無限ループを避ける
                        break;
                    }

                    _source.Position = 0;
                    continue;
                }

                total += read;
            }

            return total;
        }
    }
}
