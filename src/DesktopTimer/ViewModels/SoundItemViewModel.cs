using DesktopTimer.Models;

namespace DesktopTimer.ViewModels;

/// <summary>設定画面の音源一覧の1行。</summary>
public sealed class SoundItemViewModel : ObservableObject
{
    private bool _isPreviewing;

    public SoundItemViewModel(SoundSource source, Action<SoundItemViewModel> togglePreview)
    {
        Source = source;
        PreviewCommand = new RelayCommand(() => togglePreview(this));
    }

    public SoundSource Source { get; }

    public string Id => Source.Id;

    public string DisplayName => Source.DisplayName;

    /// <summary>削除できるか（ユーザーが追加した音源のみ）。</summary>
    public bool IsDeletable => Source.Kind == SoundKind.User;

    /// <summary>試聴中かどうか。</summary>
    public bool IsPreviewing
    {
        get => _isPreviewing;
        set
        {
            if (SetProperty(ref _isPreviewing, value))
            {
                OnPropertyChanged(nameof(PreviewLabel));
            }
        }
    }

    public string PreviewLabel => IsPreviewing ? "停止" : "試聴";

    /// <summary>試聴の開始 / 停止を切り替える。</summary>
    public RelayCommand PreviewCommand { get; }
}
