using System.Windows.Forms;

namespace RightClick.Cli;

/// <summary>Reliable clipboard write from the (STA) helper process.</summary>
internal static class Clip
{
    /// <summary>Writes text to the clipboard. Returns false if it stayed locked through every retry.</summary>
    public static bool SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        // Clipboard can momentarily be locked by another process; retry a few times.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch
            {
                if (attempt == 4)
                {
                    return false;
                }
                Thread.Sleep(80);
            }
        }
        return false;
    }
}
