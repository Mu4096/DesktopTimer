namespace DesktopTimer.ViewModels;

/// <summary>
/// 時・分・秒などの数字の入力欄を持つ画面の ViewModel。↑↓キー・ホイールでの増減を受け付ける
/// （入力欄側の処理は Views.TimeFieldBehavior）。
/// </summary>
public interface ITimeFieldStepper
{
    /// <summary>指定した欄（バインドしているプロパティ名）の値を delta だけ増減する。</summary>
    void Step(string propertyName, int delta);
}
