using System.ComponentModel;

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
                }
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
                }
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