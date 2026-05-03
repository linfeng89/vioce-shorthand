using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VoiceDiary.Animations;

public static class DeleteAnimations
{
    public static async Task AnimateDelete(View view)
    {
        try
        {
            // 1. 缩小动画
            await view.ScaleTo(0.8, 150, Easing.CubicIn);
            
            // 2. 透明度降低
            await view.FadeTo(0.3, 150, Easing.CubicOut);
            
            // 3. 向左滑动消失
            await view.TranslateTo(-view.Width, 0, 200, Easing.CubicIn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete animation error: {ex}");
        }
    }
    
    public static async Task AnimateRestore(View view)
    {
        try
        {
            // 初始状态
            view.Opacity = 0;
            view.Scale = 0.8;
            view.TranslationX = -view.Width;
            
            // 1. 从左侧滑入
            await view.TranslateTo(0, 0, 200, Easing.CubicOut);
            
            // 2. 恢复透明度
            await view.FadeTo(1, 150, Easing.CubicIn);
            
            // 3. 恢复正常大小
            await view.ScaleTo(1, 150, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restore animation error: {ex}");
        }
    }
    
    public static async Task AnimateToastIn(View view)
    {
        try
        {
            view.TranslationY = 100;
            view.Opacity = 0;
            
            await Task.WhenAll(
                view.TranslateTo(0, 0, 300, Easing.CubicOut),
                view.FadeTo(1, 300, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toast in animation error: {ex}");
        }
    }
    
    public static async Task AnimateToastOut(View view)
    {
        try
        {
            await Task.WhenAll(
                view.TranslateTo(0, 100, 300, Easing.CubicIn),
                view.FadeTo(0, 300, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toast out animation error: {ex}");
        }
    }
}
