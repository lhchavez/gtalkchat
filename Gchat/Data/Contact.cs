using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace Gchat.Data {
    [DataContract]
    public class Contact : INotifyPropertyChanged, IComparable<Contact> {
        private static object mutex = new object();

        #region Public Properties

        private string email;
        [DataMember]
        public string Email {
            get { return email; }
            set {
                if (value != email) {
                    email = value;
                    Changed("Email");
                    Changed("NameOrEmail");
                }
            }
        }

        public bool Online {
            get { return sessions.Count > 0; }
        }

        private string name;
        [DataMember]
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

        private string show = "offline";
        public string Show {
            get { return show; }
            private set {
                if (value != show) {
                    show = value;
                    Changed("Show");
                }
            }
        }

        private string photo;
        [DataMember]
        public string PhotoHash {
            get { return photo; }
            set {
                bool changed = false;

                lock (mutex) {
                    if (value != photo && value != null) {
                        photo = value;
                        changed = true;
                    }
                }

                if (changed) {
                    Changed("PhotoHash");

                    if (!string.IsNullOrEmpty(photo)) {
                        var fileName = "Shared/ShellContent/" + photo + ".jpg";

                        using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                            if (isf.FileExists(fileName)) {
                                IsolatedStorageFileStream file;

                                try {
                                    file = isf.OpenFile(fileName, FileMode.Open);
                                } catch (IsolatedStorageException e) {
                                    System.Diagnostics.Debug.WriteLine(e);
                                    return;
                                }

                                if (file.Length == 0) {
                                    file.Close();
                                    isf.DeleteFile(fileName);
                                } else {
                                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                                        try {
                                            PhotoUri = new BitmapImage();
                                            PhotoUri.SetSource(file);
                                            Changed("PhotoUri");
                                        } catch (Exception e) {
                                            System.Diagnostics.Debug.WriteLine(e);
                                        } finally {
                                            try {
                                                file.Close();
                                            } catch (Exception) { }
                                        }
                                    });

                                    return;
                                }
                            }
                        }

                        App.Current.GtalkHelper.DownloadImage(
                            this,
                            () => {
                                try {
                                    using (var isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                                        var file = isf.OpenFile(fileName, FileMode.Open);

                                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                                            try {
                                                PhotoUri = new BitmapImage();
                                                PhotoUri.SetSource(file);
                                                Changed("PhotoUri");
                                            } catch (Exception e) {
                                                System.Diagnostics.Debug.WriteLine(e);
                                            } finally {
                                                try {
                                                    file.Close();
                                                } catch (Exception) { }
                                            }
                                        });
                                    }
                                } catch (IsolatedStorageException e) {
                                    System.Diagnostics.Debug.WriteLine(e);
                                }
                            },
                            () => {
                            }
                        );
                    } else {
                        PhotoUri = null;
                        Changed("PhotoUri");
                    }
                }
            }
        }

        [DataMember]
        public Dictionary<string, ContactSession> sessions = new Dictionary<string, ContactSession>();
        public IEnumerable<ContactSession> Sessions {
            get { return sessions.Values; }
        }

        public string NameOrEmail {
            get {
                return Name ?? Email;
            }
        }

        public BitmapImage PhotoUri { get; private set; }

        public string Status {
            get {
                if (string.IsNullOrEmpty(show)) {
                    if (Online) {
                        return "available";
                    } else {
                        return "offline";
                    }
                } else if (show == "dnd") {
                    return "busy";
                } else if (show == "xa") {
                    return "away";
                } else {
                    return show;
                }
            }
        }

        private int unread;
        [DataMember]
        public int UnreadCount {
            get { return unread; }
            set {
                unread = value;
                Changed("UnreadCount");
            }
        }

        #endregion

        #region Public methods
        public void AddSession(ContactSession s) {
            sessions[s.JID] = s;
            UpdateStatusAndShow();
        }

        public void RemoveSession(string jid) {
            sessions.Remove(jid);
            UpdateStatusAndShow();
        }

        public void SetSessions(IEnumerable<ContactSession> value) {
            sessions.Clear();
            foreach(var sess in value) {
                sessions[sess.JID] = sess;
            }
            UpdateStatusAndShow();
        }

        private void UpdateStatusAndShow() {
            if (sessions.Count > 0) {
                var sess = new List<ContactSession>(sessions.Values);
                sess.Sort();
                Show = sess[0].Show;
                //Status = "";
            } else {
                Show = "offline";
                //Status = "";
            }
            Changed("Show");
            Changed("Online");
        }

        public bool Matches(string search) {
            if (Name != null && ContainsSubsequence(Name.ToLower(), search)) {
                return true;
            }

            return ContainsSubsequence(Email.ToLower(), search);
        }

        private bool ContainsSubsequence(string a, string b) {
            if (a.Length < b.Length) return false;

            for (int i = 0, j = 0; i < b.Length; i++) {
                if (!char.IsLetterOrDigit(b[i])) continue;

                bool found = false;

                for (; j < a.Length; j++) {
                    if (b[i] == a[j]) {
                        j++;
                        found = true;
                        break;
                    }
                }

                if (!found) return false;
            }

            return true;
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

        #region IComparable Members

        public int CompareTo(Contact other) {
            return CompareByName(this, other);
        }

        public static int CompareByName(Contact a, Contact b) {
            return a.NameOrEmail.CompareTo(b.NameOrEmail);
        }

        public static int CompareByStatus(Contact a, Contact b) {
            Dictionary<string, int> priority = new Dictionary<string,int> {
                {"available", 1},
                {"do not disturb", 2},
                {"away", 3},
                {"extended away", 4},
                {"offline", 5}
            };
            
            if (a.Status == b.Status) {
                return CompareByName(a, b);
            } else {
                int ast, bst;
                if (priority.TryGetValue(a.Status, out ast) && priority.TryGetValue(b.Status, out bst)) {
                    return ast.CompareTo(bst);
                }
            }

            return 0;
        }

        #endregion
    }
}