using System.ComponentModel;
using System;
using System.Net;

namespace gtalkchat {
    public class Contact : INotifyPropertyChanged {
        #region Public Properties

        private string jID;

        public string JID {
            get { return jID; }
            set {
                if (value != jID) {
                    jID = value;
                    Changed("JID");
                    Changed("Email");
                    Changed("NameOrEmail");
                }
            }
        }

        private string email;
        public string Email {
            get {
                if(email == null) {
                    email = jID;
                    if(email.Contains("/")) {
                        email = email.Substring(0, email.IndexOf('/'));
                    }
                }
                return email;
            }
        }

        private bool online;
        public bool Online {
            get { return online; }
            set {
                if (value != online) {
                    online = value;
                    Changed("Online");
                }
            }
        }

        private string name;
        public string Name {
            get { return name; }
            set {
                if (value != name) {
                    name = value;
                    Changed("Name");
                    Changed("NameOrEmail");
                }
            }
        }

        private string show;
        public string Show {
            get { return show; }
            set {
                if (value != show) {
                    show = value;
                    Changed("Show");
                }
            }
        }

        private string photo;
        public string Photo {
            get { return photo; }
            set {
                if (value != photo) {
                    photo = value;
                    Changed("Photo");
                    Changed("PhotoUri");
                }
            }
        }

        public string NameOrEmail {
            get {
                return Name ?? Email;
            }
        }

        public Uri PhotoUri {
            get {
                return new Uri("https://gtalkjsonproxy.lhchavez.com/images/" + HttpUtility.UrlEncode(Photo));
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string property) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion
    }
}