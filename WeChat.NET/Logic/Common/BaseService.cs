using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Web;
using System.Drawing;
using System.Xml;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GameSystem.BaseFunc;

namespace WeChat.NET.Protocol
{
    /// <summary>
    /// 访问http服务器类
    /// </summary>
    static class BaseService
    {
        /// <summary>
        /// GET模式从远程地址获取图片
        /// </summary>
        /// <returns></returns>
        public static Image fetchImage(string url)
        {
            Image ret = null;
            byte[] bytes = GetBytes(url);
            if (bytes != null && bytes.Length > 0)
            {
                ret = Image.FromStream(new MemoryStream(bytes));
            }
            return ret;
        }

        /// <summary>
        /// POST模式获取远程对象
        /// </summary>
        /// <param name="url"></param>
        /// <param name="_params"></param>
        /// <param name="Heads"></param>
        /// <returns></returns>
        public static JObject fetchObject(string url, JObject _params, Dictionary<string, string> Heads = null)
        {
            byte[] bytes = BaseService.PostBytes(url, _params.ToUTF8String(), Heads);
            if (bytes != null)
            {
                string init_str = Encoding.UTF8.GetString(bytes);
                if (!String.IsNullOrEmpty(init_str))
                {
                    return JsonConvert.DeserializeObject(init_str) as JObject;
                }
            }
            return null;
        }
        /// <summary>
        /// GET模式获取远程对象
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static JObject fetchObject(string url)
        {
            byte[] bytes = GetBytes(url);
            if (bytes != null)
            {
                string init_str = Encoding.UTF8.GetString(bytes);
                if (!String.IsNullOrEmpty(init_str))
                {
                    return JsonConvert.DeserializeObject(init_str) as JObject;
                }
            }
            return null;
        }
        /// <summary>
        /// GET模式获取远程调用应答中的字符串
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string fetchString(string url)
        {
            byte[] r = GetBytes(url);
            if (r != null && r.Length > 0)
            {
                return Encoding.UTF8.GetString(r);
            }
            return "";
        }

        /// <summary>
        /// 根据给定的字典，创建JObject对象
        /// </summary>
        /// <param name="list">数据字典</param>
        /// <returns></returns>
        public static JObject createJObject(Dictionary<string, object> list)
        {
            JObject ret = new JObject();
            foreach (KeyValuePair<string, object> item in list)
            {
                ret[item.Key] = JToken.FromObject(item.Value);
            }
            return ret;
        }

        /// <summary>
        /// HttpUploadFile，POST模式上传文件
        /// </summary>
        /// <param name="url">提交文件的目标地址</param>
        /// <param name="file">文件名（全路径）</param>
        /// <param name="data">附加参数列表</param>
        /// <returns></returns>
        public static JObject UploadFile(string url, JObject data, string file)
        {
            //按rfc2616规定添加的分隔符，可以是任意内容，但要确保不能在文件数据中出现
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] endbytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            //1.HttpWebRequest
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data;boundary=" + boundary;
            request.Method = "POST";
            request.Accept = "*/*";
            request.KeepAlive = true;
            //request.Referer = "https://wx.qq.com/";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Credentials = CredentialCache.DefaultCredentials;

            using (Stream stream = request.GetRequestStream())
            {
                //1.1 key/value
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                if (data != null)
                {
                    foreach (var key in data.Properties())
                    {
                        stream.Write(boundarybytes, 0, boundarybytes.Length);
                        string formitem = string.Format(formdataTemplate, key.Name, key.Value.ToUTF8String());
                        byte[] formitembytes = DEFAULTENCODE.GetBytes(formitem);
                        stream.Write(formitembytes, 0, formitembytes.Length);
                    }
                }

                //1.2 file
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type:{2}\r\n\r\n";

                stream.Write(boundarybytes, 0, boundarybytes.Length);

                string header = string.Format(headerTemplate, "filename", Path.GetFileName(file), MimeMapping.GetMimeMapping(file));
                byte[] headerbytes = DEFAULTENCODE.GetBytes(header);
                stream.Write(headerbytes, 0, headerbytes.Length);

                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    buffer.Initialize();
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        buffer.Initialize();
                    }
                }

                //1.3 form end
                stream.Write(endbytes, 0, endbytes.Length);
            }

            //2.WebResponse
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream response_stream = response.GetResponseStream();
                int count = (int)response.ContentLength;
                if (count > 0)
                {
                    int offset = 0;
                    byte[] buf = new byte[count];
                    while (count > 0)  //读取返回数据
                    {
                        int n = response_stream.Read(buf, offset, count);
                        if (n == 0) break;
                        count -= n;
                        offset += n;
                    }
                    string init_str = Encoding.UTF8.GetString(buf);
                    return JsonConvert.DeserializeObject(init_str) as JObject;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// GET模式远程获取XML文档
        /// </summary>
        /// <param name="redirect_uri"></param>
        /// <returns></returns>
        public static XmlElement fetchXmlDoc(string redirect_uri)
        {
            byte[] bytes = GetBytes(redirect_uri);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            try
            {
                string ticket = Encoding.UTF8.GetString(bytes);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ticket);
                return doc.DocumentElement;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// POST模式远程获取XML文档
        /// </summary>
        /// <param name="redirect_uri"></param>
        /// <returns></returns>
        public static XmlElement fetchXmlDoc(string redirect_uri, JObject data)
        {
            byte[] bytes = PostBytes(redirect_uri, data.ToUTF8String());
            if (bytes == null)
            {
                return null;
            }
            try
            {
                string ticket = Encoding.UTF8.GetString(bytes);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ticket);
                return doc.DocumentElement;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取指定cookie
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Cookie GetCookie(string name)
        {
            List<Cookie> cookies = GetAllCookies(CookiesContainer);
            foreach (Cookie c in cookies)
            {
                if (c.Name == name)
                {
                    return c;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 默认编码方案
        /// </summary>
        private static readonly Encoding DEFAULTENCODE = Encoding.UTF8;
        /// <summary>
        /// 访问服务器时的cookies
        /// </summary>
        private static CookieContainer CookiesContainer;

        /// <summary>
        /// 向服务器发送get请求  读取并返回字节数组
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static byte[] GetBytes(string url)
        {
            try
            {
                if (Program.isDebug)
                {
                    Console.WriteLine("发起访问：" + url);
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                //request.Referer = "https://wx.qq.com/";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36";
                request.Accept = "*/*";
                request.KeepAlive = true;
                request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, sdch, br");
                if (CookiesContainer == null)
                {
                    CookiesContainer = new CookieContainer();
                }
                request.CookieContainer = CookiesContainer;  //启用cookie

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    Stream response_stream = response.GetResponseStream();

                    int count = (int)response.ContentLength;
                    if (count > 0)
                    {
                        int offset = 0;
                        byte[] buf = new byte[count];
                        while (count > 0)  //读取返回数据
                        {
                            int n = response_stream.Read(buf, offset, count);
                            if (n == 0) break;
                            count -= n;
                            offset += n;
                        }
                        return buf;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// GET模式获取远程调用应答中的字符串
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string SimpleGetStr(string url, JObject data)
        {
            url += data.ToUrl();

            if (Program.isDebug)
            {
                Console.WriteLine("发起访问：" + url);
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36";
            request.Accept = "*/*";
            request.KeepAlive = true;
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, sdch, br");
            request.Credentials = CredentialCache.DefaultCredentials;
            if (CookiesContainer == null)
            {
                CookiesContainer = new CookieContainer();
            }
            request.CookieContainer = CookiesContainer;  //启用cookie

            using (WebResponse response = request.GetResponse())
            {
                string reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                try
                {
                    request.GetResponse().Close();
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return reader;
            }
        }

        /// <summary>
        /// POST模式获取远程调用应答中的字符串
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static string fetchString(string url, JObject body)
        {
            try
            {
                byte[] request_body = Encoding.UTF8.GetBytes(body.ToUTF8String());

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";

                if (CookiesContainer == null)
                {
                    CookiesContainer = new CookieContainer();
                }
                request.CookieContainer = CookiesContainer;  //启用cookie
                Stream request_stream = request.GetRequestStream();
                request_stream.Write(request_body, 0, request_body.Length);

                using (WebResponse response = request.GetResponse())
                {
                    string reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                    try
                    {
                        request.GetResponse().Close();
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    return reader;
                }
            }
            catch
            {
            }
            return "";
        }

        /// <summary>
        /// 向服务器发送post请求 读取并返回字节数组
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private static byte[] PostBytes(string url, string body, Dictionary<string, string> Heads = null)
        {
            try
            {
                if (Program.isDebug)
                {
                    Console.WriteLine("发起访问：" + url);
                }

                byte[] request_body = Encoding.UTF8.GetBytes(body);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                if (Heads != null)
                {
                    foreach (var item in Heads)
                    {
                        request.Headers.Add(item.Key, item.Value);
                    }
                }
                request.Credentials = CredentialCache.DefaultCredentials;
                request.ContentType = "application/json; charset=UTF-8";
                request.ContentLength = request_body.Length;
                if (CookiesContainer == null)
                {
                    CookiesContainer = new CookieContainer();
                }
                request.CookieContainer = CookiesContainer;  //启用cookie

                Stream request_stream = request.GetRequestStream();
                request_stream.Write(request_body, 0, request_body.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    Stream response_stream = response.GetResponseStream();
                    int count = (int)response.ContentLength;
                    if (count > 0)
                    {
                        int offset = 0;
                        byte[] buf = new byte[count];
                        while (count > 0)  //读取返回数据
                        {
                            int n = response_stream.Read(buf, offset, count);
                            if (n == 0) break;
                            count -= n;
                            offset += n;
                        }
                        return buf;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private static List<Cookie> GetAllCookies(CookieContainer cc)
        {
            List<Cookie> lstCookies = new List<Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies) lstCookies.Add(c);
            }
            return lstCookies;
        }

        /// <summary>
        /// GET模式远程获取文件并写入本地
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string fetchFile(string url, string fileName)
        {
            byte[] r = GetBytes(url);
            if (r != null && r.Length > 0)
            {
                try
                {
                    File.WriteAllBytes(fileName, r);
                }
                catch { }
                return fileName;
            }
            return "";
        }
    }
}
