/****************************************************
文件：FileUtil
作者：haitao.li
日期：2023/02/06 17:41:39
功能：文件帮助类
*****************************************************/

using System.IO;

namespace ResourceTools
{
    public class FileUtil
    {
        public static void CreateFile(string filePath, string data)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.Write(data);
            }
        }

        public static void CreateFile<T>(string filePath, T obj)
        {
            //写入清单文件json
            string data =  CustomJson.JsonParser.ToJson(obj);
            CreateFile(filePath, data);
        }
    }
}