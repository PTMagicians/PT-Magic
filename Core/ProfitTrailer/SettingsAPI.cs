using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using Core.Main;
using Core.Helper;

namespace Core.ProfitTrailer
{
  public static class SettingsAPI
  {
    // Save config back to Profit Trailer
    public static void SendPropertyLinesToAPI(List<string> pairsLines, List<string> dcaLines, List<string> indicatorsLines, PTMagicConfiguration systemConfiguration, LogHelper log)
    {
      int retryCount = 0;
      int maxRetries = 3;
      bool transferCompleted = false;
      bool transferCanceled = false;

      // get PT license list
      string licenses = systemConfiguration.GeneralSettings.Application.ProfitTrailerLicense;
      if (systemConfiguration.GeneralSettings.Application.ProfitTrailerLicenseXtra != "")
      {
        licenses = licenses + ", " + systemConfiguration.GeneralSettings.Application.ProfitTrailerLicenseXtra;
      }
      List<string> licenseList = SystemHelper.ConvertTokenStringToList(licenses, ",");
      int licenseCount = licenseList.Count;

      // get URL list
      string urls = systemConfiguration.GeneralSettings.Application.ProfitTrailerMonitorURL;
      if (systemConfiguration.GeneralSettings.Application.ProfitTrailerMonitorURLXtra != "")
      {
        urls = urls + ", " + systemConfiguration.GeneralSettings.Application.ProfitTrailerMonitorURLXtra;
      }
      List<string> urlList = SystemHelper.ConvertTokenStringToList(urls, ",");
      int urlCount = urlList.Count;

      log.DoLogInfo("Found " + licenseCount + " licenses and " + urlCount + " URLs");
      if (urlCount != licenseCount)
      {
        log.DoLogWarn("ERROR - urlCount must match licenseCount");
      }
      for (int i = 0; i < licenseCount; i++)
      {
        transferCompleted = false;
        while (!transferCompleted && !transferCanceled)
        {
          try
          {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(CertificateHelper.AllwaysGoodCertificate);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(urlList[i] + "settingsapi/settings/saveAll");
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.Proxy = null;
            httpWebRequest.Timeout = 30000;

            // PT is using ordinary POST data, not JSON
            string query = "configName=" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName;
            query = query + "&license=" + licenseList[i];
            string pairsPropertiesString = SystemHelper.ConvertListToTokenString(pairsLines, Environment.NewLine, false);
            string dcaPropertiesString = SystemHelper.ConvertListToTokenString(dcaLines, Environment.NewLine, false);
            string indicatorsPropertiesString = SystemHelper.ConvertListToTokenString(indicatorsLines, Environment.NewLine, false);
            query += "&pairsData=" + WebUtility.UrlEncode(pairsPropertiesString) + "&dcaData=" + WebUtility.UrlEncode(dcaPropertiesString) + "&indicatorsData=" + WebUtility.UrlEncode(indicatorsPropertiesString);
            byte[] formData = Encoding.ASCII.GetBytes(query);
            httpWebRequest.ContentLength = formData.Length;
            using (Stream stream = httpWebRequest.GetRequestStream())
            {
              stream.Write(formData, 0, formData.Length);
            }
            log.DoLogDebug("Built POST request for Properties");
            int adjustedCount = i+1;
            log.DoLogInfo("Sending Properties to license " + adjustedCount + " at " + urlList[i]);
            using (HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
              log.DoLogInfo("Properties sent!");
              httpResponse.Close();
              log.DoLogDebug("Properties response object closed.");
            }
            
            transferCompleted = true;
          }
          catch (WebException ex)
          {
            // Manual error handling as PT doesn't seem to provide a proper error response...
            if (ex.Message.IndexOf("401") > -1)
            {
              log.DoLogError("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': Unauthorized! The specified Profit Trailer license key '" + systemConfiguration.GetProfitTrailerLicenseKeyMasked() + "' is invalid!");
              transferCanceled = true;
            }
            else if (ex.Message.IndexOf("timed out") > -1)
            {
              // Handle timeout seperately
              retryCount++;
              if (retryCount <= maxRetries)
              {
                log.DoLogError("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': Timeout! Starting retry number " + retryCount + "/" + maxRetries.ToString() + "!");
              }
              else
              {
                transferCanceled = true;
                log.DoLogError("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': Timeout! Canceling transfer after " + maxRetries.ToString() + " failed retries.");
              }
            }
            else
            {
              log.DoLogCritical("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': " + ex.Message, ex);
              transferCanceled = true;
            }

          }
          catch (TimeoutException ex)
          {
            retryCount++;
            if (retryCount <= maxRetries)
            {
              log.DoLogError("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': Timeout (" + ex.Message + ")! Starting retry number " + retryCount + "/" + maxRetries.ToString() + "!");
            }
            else
            {
              transferCanceled = true;
              log.DoLogError("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': Timeout (" + ex.Message + ")! Canceling transfer after " + maxRetries.ToString() + " failed retries.");
            }
          }
          catch (Exception ex)
          {
            log.DoLogCritical("Saving Properties failed for setting '" + systemConfiguration.GeneralSettings.Application.ProfitTrailerDefaultSettingName + "': " + ex.Message, ex);
            transferCanceled = true;
          }
        }
      }


    }
  }
}