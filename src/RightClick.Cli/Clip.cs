using System.Windows.Forms;

namespace RightClick.Cli;

/// <summary>Reliable clipboard write from the (STA) helper process.</summary>
internal static class Clip
{
    public static void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Clipboard can momentarily be locked by another process; retry a few times.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch when (attempt < 4)
            {
                Thread.Sleep(80);
            }
        }
    }
}
