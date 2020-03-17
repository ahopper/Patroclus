using System;
using System.IO;

namespace Patroclus.Avalonia
{
	/// <summary>
	/// Summary description for errorlog.
	/// </summary>
	public class Errorlog
	{
		public Errorlog()
		{
		}
        private static string s_filename=null;

		public static void logException(Exception e)
		{
			logException(e,"");

		}
        public static string LogDirectory
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                    Path.DirectorySeparatorChar + "m0nnb" + Path.DirectorySeparatorChar +
                    "Patroclus" + Path.DirectorySeparatorChar + "errorlogs" + Path.DirectorySeparatorChar;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string filename
        {
            get
            {
                if(s_filename==null)
                {                  
                    s_filename = LogDirectory + "log-"+DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") +".txt";
                }
                return s_filename;
            }
        }
		public static void logException(Exception e,string msg)
		{
	        try
            {
                using (StreamWriter sw = File.AppendText(filename))
                {
                    sw.WriteLine(DateTime.Now.ToString());
                    sw.WriteLine(msg);
                    while (e != null)
                    {
                        sw.WriteLine(e.Message);
                        sw.WriteLine(e.Source);
                        sw.WriteLine(e.TargetSite);
                        sw.WriteLine(e.StackTrace);
                        e = e.InnerException;
                    }

                    sw.WriteLine("--");
                }
            }
            catch (Exception)
            {

            }
		}
        public static void logMessage(string msg)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(filename))
                {
                    sw.WriteLine(DateTime.Now.ToString());
                    sw.WriteLine(msg);

                }
            }
            catch(Exception )
            {

            }
        }
	}
}
