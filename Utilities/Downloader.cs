using System;
using System.Net;
using System.Text;
using System.IO;

public class Downloader : WebClient
{
    private int timeout;

    private Downloader(int timeout)
    {
        this.timeout = timeout;
    }

    protected override WebRequest GetWebRequest(Uri uri)
    {
        var result = base.GetWebRequest(uri);
        result.Timeout = this.timeout;
        return result;
    }

    public static void Download(string url, string path, int timeout)
    {
        string download_folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(download_folder))
        {
            Directory.CreateDirectory(download_folder);
        }

        using (Downloader web_client = new Downloader(timeout))
        {
            byte[] data = web_client.DownloadData(url);
            if ((data != null) && (data.Length > 0))
            {
                File.WriteAllBytes(path, data);
            }
            else
            {
                throw new Exception("Invalid server address.\r\nPlease correct address in .INI file.");
            }
        }
    }

    //private void TestButton_Click(object sender, EventArgs e)
    //{
    //    this.Cursor = Cursors.WaitCursor;
    //    try
    //    {
    //        string filename = "Downloads/Manual.pdf";
    //        if (!File.Exists(filename))
    //        {
    //            Downloader.Download("http://heliwave.com/Soul.and.Spirit.pdf", "Downloads/Manual.pdf", 10000);
    //        }
    //        if (File.Exists(filename))
    //        {
    //            System.Diagnostics.Process.Start(filename);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        while (ex != null)
    //        {
    //            Console.WriteLine(ex.Message);
    //            MessageBox.Show(ex.Message, Application.ProductName);
    //            ex = ex.InnerException;
    //        }
    //    }
    //    finally
    //    {
    //        this.Cursor = Cursors.Default;
    //    }
    //}
}
