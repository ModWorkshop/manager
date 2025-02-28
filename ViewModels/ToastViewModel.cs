using ReactiveUI.SourceGenerators;
using Splat;

namespace MWSManager.ViewModels;

public enum ToastType
{
    Info,
    Warning,
    Danger,
    Success,
}

public partial class ToastViewModel : ViewModelBase
{
    [Reactive] private bool isClosing = false;
    public string Icon => Type switch {
        ToastType.Info => "mdi-information",
        ToastType.Warning => "mdi-alert-box",
        ToastType.Danger => "mdi-alert",
        ToastType.Success => "mdi-checkbox-marked",
        _ => Icon
    };

    // Style selectors basically
    public bool IsWarning => Type == ToastType.Warning;
    public bool IsDanger => Type == ToastType.Danger;
    public bool IsSuccess => Type == ToastType.Success;
    public bool IsInfo => Type == ToastType.Info;
    
    [Reactive] private string title;
    [Reactive] private string desc;

    [Reactive] private ToastType type;

    public int TimeMiliseconds { get; private set; }

    public ToastViewModel(ToastType type, string title, string desc, int timeMiliseconds = 3000)
    {
        this.title = title;
        this.desc = desc;
        
        this.type = type;
        
        TimeMiliseconds = timeMiliseconds;
    }

    [ReactiveCommand]
    public void CloseToast()
    {
        Locator.Current.GetService<ToasterViewModel>()?.RemoveToast(this);
    }
 }