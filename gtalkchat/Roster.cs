using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace gtalkchat {
    public class Roster : ObservableCollection<Contact> {
        private readonly Dictionary<string, Contact> contacts;

        public Roster() {
            contacts = new Dictionary<string, Contact>();
        }

        public new void Add(Contact item) {
            base.Add(item);

            var email = item.JID;

            if(email.Contains("/")) {
                email = email.Substring(0, email.IndexOf('/'));
            }

            contacts.Add(email, item);
        }

        public bool Contains(string email) {
            return contacts.ContainsKey(email);
        }

        public Contact this[string email] {
            get { return contacts[email]; }
            set { contacts[email] = value; }
        }
    }
}
