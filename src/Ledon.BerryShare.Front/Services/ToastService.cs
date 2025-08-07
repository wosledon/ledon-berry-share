using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Ledon.BerryShare.Front.Services
{
    public enum ToastType { Info, Warn, Error }

    public class ToastService
    {
        public event Func<string, ToastType, Task>? OnShow;

        public async Task ShowToastAsync(string message, ToastType type = ToastType.Info)
        {
            if (OnShow != null)
                await OnShow.Invoke(message, type);
        }
    }
}
