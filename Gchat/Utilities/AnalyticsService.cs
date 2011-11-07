using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Coding4Fun.Phone.Controls.Data;
using Microsoft.Phone.Info;
using Google.WebAnalytics;
using Microsoft.WebAnalytics.Data;
using Microsoft.WebAnalytics;
using System.ComponentModel.Composition.Hosting;
using Microsoft.WebAnalytics.Behaviors;
using System.ComponentModel.Composition;

namespace Gchat.Utilities {
    public class AnalyticsService : IApplicationService {
        private readonly IApplicationService _innerService;
        private readonly GoogleAnalytics _googleAnalytics;

        public AnalyticsService() {
            _googleAnalytics = new GoogleAnalytics();
            _googleAnalytics.CustomVariables.Add(new PropertyValue { PropertyName = "Device ID", Value = AnalyticsProperties.DeviceId });
            _googleAnalytics.CustomVariables.Add(new PropertyValue { PropertyName = "Application Version", Value = AnalyticsProperties.ApplicationVersion });
            _googleAnalytics.CustomVariables.Add(new PropertyValue { PropertyName = "Device OS", Value = AnalyticsProperties.OsVersion });
            _googleAnalytics.CustomVariables.Add(new PropertyValue { PropertyName = "Device", Value = AnalyticsProperties.Device });
            _innerService = new WebAnalyticsService {
                IsPageTrackingEnabled = false,
                Services = { _googleAnalytics, }
            };
        }

        public string WebPropertyId {
            get { return _googleAnalytics.WebPropertyId; }
            set { _googleAnalytics.WebPropertyId = value; }
        }

        #region IApplicationService Members

        public void StartService(ApplicationServiceContext context) {
            CompositionHost.Initialize(new AssemblyCatalog(
                Application.Current.GetType().Assembly),
                new AssemblyCatalog(typeof(AnalyticsEvent).Assembly),
                new AssemblyCatalog(typeof(TrackAction).Assembly));
            _innerService.StartService(context);
        }

        public void StopService() {
            _innerService.StopService();
        }

        #endregion
    }

    public static class AnalyticsProperties {
        public static string DeviceId {
            get {
                var value = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
                return Convert.ToBase64String(value);
            }
        }

        public static string DeviceManufacturer {
            get { return DeviceExtendedProperties.GetValue("DeviceManufacturer").ToString(); }
        }

        public static string DeviceType {
            get { return DeviceExtendedProperties.GetValue("DeviceName").ToString(); }
        }

        public static string Device {
            get { return string.Format("{0} - {1}", DeviceManufacturer, DeviceType); }
        }

        public static string OsVersion {
            get { return string.Format("WP {0}", Environment.OSVersion.Version); }
        }

        public static string ApplicationVersion {
            get {
                var version = PhoneHelper.GetAppAttribute("Version").Replace(".0.0", "");
                if (GoogleTalkHelper.IsPaid()) {
                    version += " (Paid)";
                }
                return version;
            }
        }
    }
}
