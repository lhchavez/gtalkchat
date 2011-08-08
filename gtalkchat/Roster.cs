using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

namespace gtalkchat {
    public class Roster : List<Contact>, INotifyCollectionChanged {
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

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [DataMember]
        public readonly Dictionary<string, Contact> contacts;
        private bool pendingNotify;

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
            App.Current.Settings["roster"] = this;
        }

        private static string GetEmail(string jid) {
            var email = jid;

            if (email.Contains("/")) {
                email = email.Substring(0, email.IndexOf('/'));
            }

            return email;
        }
    }
}
