using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Gchat.Utilities;

namespace Gchat.Data {
    public class Roster : List<Contact>, INotifyCollectionChanged {
        #region Public Properties

        private bool notify;
        public bool Notify {
            get { return notify; }
            set {
                if(!notify && value && pendingNotify && CollectionChanged != null) {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                pendingNotify = false;
                notify = value;
            }
        }

        #endregion

        #region Public Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Private Fields

        private readonly Dictionary<string, Contact> contacts;
        private bool pendingNotify;

        #endregion

        #region Public Methods

        public Roster() {
            contacts = new Dictionary<string, Contact>();
            Notify = true;
        }

        public new void Add(Contact item) {
            base.Add(item);
            Sort();
            contacts.Add(item.Email, item);

            if (Notify) {
                if (CollectionChanged != null) {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Count - 1));
                }
            } else {
                pendingNotify = true;
            }
        }

        public new void Clear() {
            base.Clear();
            contacts.Clear();

            if (Notify) {
                if (CollectionChanged != null) {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            } else {
                pendingNotify = true;
            }
        }

        public bool Contains(string jid) {
            return contacts.ContainsKey(GetEmail(jid));
        }

        public Contact this[string jid] {
            get {
                if (Contains(jid)) {
                    return contacts[GetEmail(jid)];
                } else {
                    return null;
                }
            }
            set { contacts[GetEmail(jid)] = value; }
        }

        public ObservableCollection<Contact> GetOnlineContacts() {
            var online = this.Where(r => r.Online).ToObservableCollection();
            online.Sort(Contact.CompareByStatus);
            return online;
        }

        public void Save() {
            var ser = new DataContractJsonSerializer(typeof(Contact));

            bool first = true;

            using (var ms = new MemoryStream()) {
                foreach (var contact in this) {
                    if (!first) {
                        ms.WriteByte((byte) '\n');
                    }
                    first = false;

                    ser.WriteObject(ms, contact);
                }

                var buf = ms.GetBuffer();

                App.Current.Settings["roster"] = Encoding.UTF8.GetString(buf, 0, (int)ms.Position);
            }
        }

        public void Load() {
            if (!App.Current.Settings.Contains("roster")) return;

            var serialized = App.Current.Settings["roster"] as string;

            if (string.IsNullOrEmpty(serialized)) return;

            var ser = new DataContractJsonSerializer(typeof(Contact));

            foreach (var line in serialized.Split(new[] {'\n'})) {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(line))) {
                    Add(ser.ReadObject(ms) as Contact);
                }
            }
        }

        #endregion

        #region Private Methods

        private static string GetEmail(string jid) {
            var email = jid;

            if (email.Contains("/")) {
                email = email.Substring(0, email.IndexOf('/'));
            }

            return email;
        }

        #endregion
    }
}
