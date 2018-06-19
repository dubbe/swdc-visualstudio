﻿namespace SoftwareCo
{
    internal static class Constants
    {
        internal const string PluginName = "swdc-visualstudio";
        internal static string PluginVersion = "0.1.4";
        internal const string EditorName = "visualstudio";

        internal const string TEST_API_ENDPOINT = "http://localhost:5000";
        internal const string TEST_URL_ENDPOINT = "http://localhost:3000";
        internal const string PROD_API_ENDPOINT = "https://api.software.com";
        internal const string PROD_URL_ENDPOINT = "https://alpha.software.com";

        internal const string api_endpoint = PROD_API_ENDPOINT;
        internal const string url_endpoint = PROD_URL_ENDPOINT;

        internal static string EditorVersion
        {
            get
            {
                if (SoftwareCoPackage.ObjDte == null)
                {
                    return string.Empty;
                }
                return SoftwareCoPackage.ObjDte.Version;
            }
        }
    }
}