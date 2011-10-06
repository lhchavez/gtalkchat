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
                if (value != photo) {
                    photo = value;
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
            if (Name != null) {
                return Name.ToLower().Contains(search.ToLower());
            } else {
                return Email.ToLower().Contains(search.ToLower());
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