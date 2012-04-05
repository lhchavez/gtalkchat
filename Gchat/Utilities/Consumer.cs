using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media.Imaging;
using Gchat.Data;
using System.IO.IsolatedStorage;
using System.IO;
using System.ComponentModel;

namespace Gchat.Utilities {
    public class Consumer {
        private ManualResetEvent mutex;
        private ManualResetEvent serializer;
        private Queue<ConsumerElement> q;
        private BackgroundWorker worker;
        private bool alive;

        public Consumer() {
            mutex = new ManualResetEvent(false);
            serializer = new ManualResetEvent(true);
            q = new Queue<ConsumerElement>();
            alive = true;

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(Run);
            worker.RunWorkerAsync();
        }

        ~Consumer() {
            try {
                alive = false;
                mutex.Set();
            } catch (Exception) {
                // Hope for the best.
            }
        }

        public void Run(object sender, DoWorkEventArgs args) {
            while (alive) {
                mutex.WaitOne();
                mutex.Reset();

                if (!alive) return;

                while (q.Count > 0) {
                    serializer.WaitOne();
                    serializer.Reset();

                    ConsumerElement element = q.Dequeue();

                    var fileName = "Shared/ShellContent/" + element.PhotoHash + ".jpg";

                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                        bool finished = false;

                        using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                            if (isf.FileExists(fileName)) {
                                try {
                                    var file = isf.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                                    if (file.Length == 0) {
                                        file.Close();
                                        isf.DeleteFile(fileName);
                                    } else {
                                        try {
                                            var PhotoUri = new BitmapImage();
                                            PhotoUri.SetSource(file);

                                            element.SuccessCallback(element.PhotoHash, PhotoUri);
                                            serializer.Set();

                                            file.Close();

                                            finished = true;
                                        } catch (Exception e) {
                                            System.Diagnostics.Debug.WriteLine(e);

                                            file.Close();
                                            isf.DeleteFile(fileName);
                                        }
                                    }
                                } catch (Exception e) {
                                    System.Diagnostics.Debug.WriteLine(e);
                                }
                            }
                        }

                        if (!finished) {
                            App.Current.GtalkHelper.DownloadImage(
                                element.Contact,
                                () => App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                                        try {
                                            using (var file = isf.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                                                var PhotoUri = new BitmapImage();
                                                PhotoUri.SetSource(file);
                                                element.SuccessCallback(element.PhotoHash, PhotoUri);
                                                serializer.Set();
                                            }
                                        } catch (Exception e) {
                                            System.Diagnostics.Debug.WriteLine(e);
                                            element.ErrorCallback("");
                                            serializer.Set();
                                        }
                                    }
                                }),
                                () => {
                                    serializer.Set();
                                    element.ErrorCallback("");
                                }
                            );
                        }
                    });
                }
            }
        }

        public void Add(Contact c, Action<string, BitmapImage> success, Action<string> error) {
            q.Enqueue(new ConsumerElement() {
                PhotoHash = c.PhotoHash,
                Contact = c,
                SuccessCallback = success,
                ErrorCallback = error
             });

            mutex.Set();
        }

        private struct ConsumerElement {
            public string PhotoHash;
            public Contact Contact;
            public Action<string, BitmapImage> SuccessCallback;
            public Action<string> ErrorCallback;
        };
    }
}
