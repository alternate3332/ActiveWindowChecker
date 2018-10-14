using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

class FileWriter
{

    public enum FileWriter_RESULT
    {
        SUCCESS,
        FILE_OPEN_ERROR,
        UNKNOWN_ERROR,
        INPUT_DATA_ERROR,
        READ_ONLY_FILE_ERROR
    };

    static private string tmpPath = "tmp.csv";
    //private string dstPath = "C:/Users/" + System.Environment.UserName + "/Desktop/ActiveWindowLog.csv";
    static private string dstPath = null;

    static public FileWriter_RESULT init(string dstpath)
    {
        string date = DateTime.Now.ToString("yyyyMMddHHmmss");
        dstPath = dstpath;

        if(dstPath == null)
        {
            //dstPath = "C:/Users/" + System.Environment.UserName + "/Desktop/ActiveWindowLog_" + date + ".csv";
            dstPath = "C:/Users/智博/Desktop/ActiveWindowLog_" + date + ".csv";

        }

        Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
        StreamWriter sw = null;
        try
        {
            sw = new StreamWriter(tmpPath, false, utf8Enc);
        }
        catch (Exception)
        {
            return FileWriter_RESULT.FILE_OPEN_ERROR;

        }
        
        sw.Close();


        try
        {
            System.IO.File.Copy(tmpPath, dstPath, true);

        }
        catch (Exception)
        {
            return FileWriter_RESULT.FILE_OPEN_ERROR;
        }

        return FileWriter_RESULT.SUCCESS;
    }

    static public FileWriter_RESULT write(string[] datas)
    {
        if(datas == null)
        {
            return FileWriter_RESULT.INPUT_DATA_ERROR;
        }

        string data = string.Join(",", datas);

        Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
        StreamWriter sw = null;

        try
        {
            sw = new StreamWriter(tmpPath, true, utf8Enc);
            sw.WriteLine(data);
            sw.Close();
            System.IO.File.Copy(tmpPath, dstPath, true);
        }
        catch (IOException)
        {
            return FileWriter_RESULT.FILE_OPEN_ERROR;

        }
        catch (UnauthorizedAccessException)
        {
            System.IO.File.Copy(tmpPath, dstPath, true);
            return FileWriter_RESULT.READ_ONLY_FILE_ERROR;
        }


      

        return FileWriter_RESULT.SUCCESS;
    }
}

