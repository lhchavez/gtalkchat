using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace gtalkchat {
    public class Roster : ObservableCollection<Contact> {
        private readonly Dictionary<string, Contact> contacts;

        public Roster() {
            contacts = new Dictionary<string, Contact>();
        }

        public new void Add(Contact item) {
            base.Add(item);
            contacts.Add(GetEmail(item.JID), item);
        }

        public bool Contains(string jid) {
            return contacts.ContainsKey(GetEmail(jid));
        }

        public Contact this[string jid] {
            get { return contacts[GetEmail(jid)]; }
            set { contacts[GetEmail(jid)] = value; }
        }

        public ObservableCollection<Contact> GetOnlineContacts() {
            return this.Where(r => r.Online).ToObservableCollection();
        }

        private string GetEmail(string jid) {
            var email = jid;

            if (email.Contains("/")) {
                email = email.Substring(0, email.IndexOf('/'));
            }

            return email;
        }
    }
}
