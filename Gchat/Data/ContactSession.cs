using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gchat.Data {
    [DataContract]
    public class ContactSession : INotifyPropertyChanged, IComparable<ContactSession> {
        #region Public properties

        private string jid;
        [DataMember]
        public string JID {
            get { return jid; }
            set {
                if(jid != value) {
                    jid = value;
                    Changed("JID");
                }
            }
        }

        private string show = "";
        [DataMember]
        public string Show {
            get { return show; }
            set {
                if (show != (value ?? "")) {
                    show = value ?? "";
                    Changed("Show");
                }
            }
        }

        private string status;
        [DataMember]
        public string Status {
            get { return status; }
            set {
                if (status != value) {
                    status = value;
                    Changed("Status");
                }
            }
        }

        private List<string> capabilities = new List<string>();
        [DataMember]
        public List<string> Capabilities {
            get { return capabilities; }
            set {
                if(!value.Equals(capabilities)) {
                    capabilities = value;
                    Changed("Capabilities");
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string property) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion

        #region IComparable<ContactSession> members
        int IComparable<ContactSession>.CompareTo(ContactSession other) {
            return CompareByShow(this, other);
        }

        public static int CompareByShow(ContactSession a, ContactSession b) {
            Dictionary<string, int> priority = new Dictionary<string, int> {
                {"available", 1},
                {"", 1},
                {"dnd", 2},
                {"away", 3},
                {"xa", 4}
            };

            if (a.Show != b.Show) {
                int ast, bst;
                if (priority.TryGetValue(a.Show, out ast) && priority.TryGetValue(b.Show, out bst)) {
                    return ast.CompareTo(bst);
                }
            }

            return 0;
        }

        #endregion
    }
}
