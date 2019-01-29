﻿

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
// using SpotifyAPI.Local;
// using SpotifyAPI.Local.Enums;
// using SpotifyAPI.Local.Models;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        private static bool _telemetryOn = true;

        /***
        private SpotifyLocalAPI _spotify = null;

        public IDictionary<string, string> getTrackInfo()
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();

            if (_spotify == null)
            {
                _spotify = new SpotifyLocalAPI();
            }

            if (SpotifyLocalAPI.IsSpotifyRunning() && _spotify.Connect())
            {
                StatusResponse status = _spotify.GetStatus();
                Logger.Info("got spotify status: " + status.ToString());
            }

            return dict;
        }
        **/

        public static string RunCommand(String cmd, String dir)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + cmd;
                process.StartInfo.WorkingDirectory = dir;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                //* Read the output (or the error)
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (output != null)
                {
                    return output.Trim();
                }
            } catch (Exception e)
            {
                Logger.Error("Software.com: Unable to execute command, error: " + e.Message);
            }
            return "";
        }

        public static void UpdateTelemetry(bool isOn)
        {
            _telemetryOn = isOn;
        }

        public static bool isTelemetryOn()
        {
            return _telemetryOn;
        }

        public static object getItem(string key)
        {
            // read the session json file
            string sessionFile = getSoftwareSessionFile();
            if (File.Exists(sessionFile))
            {
                string content = File.ReadAllText(sessionFile);
                if (content != null)
                {
                    object val = SimpleJson.GetValue(content, key);
                    if (val != null)
                    {
                        return val;
                    }
                }
            }
            return null;
        }

        public static void setItem(String key, object val)
        {
            string sessionFile = getSoftwareSessionFile();
            IDictionary<string, object> dict = new Dictionary<string, object>();
            string content = "";
            if (File.Exists(sessionFile))
            {
                content = File.ReadAllText(sessionFile);
                // conver to dictionary
                dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content);
                dict.Remove(key);
            }
            dict.Add(key, val);
            content = SimpleJson.SerializeObject(dict);
            // write it back to the file
            File.WriteAllText(sessionFile, content);
        }
        
        public static String getSoftwareDataDir()
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (!Directory.Exists(softwareDataDir))
            {
                // create it
                Directory.CreateDirectory(softwareDataDir);
            }
            return softwareDataDir;
        }

        public static String getSoftwareSessionFile()
        {
            return getSoftwareDataDir() + "\\session.json";
        }

        public static String getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir() + "\\data.json";
        }

        public static void launchSoftwareDashboard()
        {
            string url = Constants.url_endpoint;
            object tokenVal = getItem("token");
            object jwtVal = getItem("jwt");

            bool addedToken = false;
            if (tokenVal == null || ((string)tokenVal).Equals(""))
            {
                tokenVal = createToken();
                setItem("token", tokenVal);
                addedToken = true;
            }
            else if (jwtVal == null || ((string)jwtVal).Equals(""))
            {
                addedToken = true;
            }

            if (addedToken)
            {
                url += "/login?token=" + (string)tokenVal;
                RetrieveAuthTokenTimeout(60000);
            }

           Process.Start(url);
        }

        public static string createToken()
        {
            return System.Guid.NewGuid().ToString().Replace("-", "");
        }

        public static async void RetrieveAuthTokenTimeout(int millisToWait)
        {
            await Task.Delay(millisToWait);
            RetrieveAuthToken();
        }

        public static async void RetrieveAuthToken()
        {
            object token = getItem("token");
            string jwt = null;
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/users/plugin/confirm?token=" + token, null);
            if (SoftwareHttpManager.IsOk(response))
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                jsonObj.TryGetValue("jwt", out object jwtObj);
                jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);

                if (jwt != null)
                {
                    setItem("jwt", jwt);
                }

                setItem("vs_lastUpdateTime", getNowInSeconds());
            }

            if (jwt == null)
            {
                RetrieveAuthTokenTimeout(120000);
            }
        }

        public static long getNowInSeconds()
        {
            long unixSeconds = DateTimeOffset.Now.ToUnixTimeSeconds();
            return unixSeconds;
        }
    }

    
}
