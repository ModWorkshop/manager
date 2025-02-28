using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Animation;
using Serilog;

namespace MWSManager.ViewModels;

public class ToasterViewModel : ViewModelBase
{
    public ObservableCollection<ToastViewModel> Toasts { get; } = [];

    public ToasterViewModel()
    {
        Console.WriteLine("ToasterViewModel!!");
    }

    public void RemoveToast(ToastViewModel toast)
    {
        toast.IsClosing = true;
        var timer = new Timer(250);
        timer.Elapsed += (sender, args) =>
        {
            Toasts.Remove(toast);
            timer.Dispose();
        };
        timer.Start();
    }
    
    public void AddToast(ToastViewModel toast)
    {
        Toasts.Add(toast);
        var timer = new Timer(300000);
        timer.Elapsed += (sender, args) =>
        {
            if (toast.IsClosing) return;
            timer.Dispose();
            RemoveToast(toast);
        };
        timer.Start();
    }
}
